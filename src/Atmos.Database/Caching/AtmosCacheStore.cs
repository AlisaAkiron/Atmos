using Atmos.Domain.Entities.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;

namespace Atmos.Database.Caching;

/// <summary>
/// Key/value operations over the <c>atmos_cache</c> table. Shared by the
/// <c>IDistributedCache</c> and output cache implementations, which differ only in
/// how they name keys and how they treat failures.
/// </summary>
/// <remarks>
/// Registered as a singleton, so every operation rents its own scope rather than
/// holding a <see cref="AtmosDbContext"/>: cache calls arrive from anywhere,
/// including background services with no ambient scope.
/// </remarks>
public sealed class AtmosCacheStore
{
    /// <summary>
    /// A single upsert rather than update-then-insert. Two instances racing the
    /// first write of a key is normal on a cold cache, and losing that race the
    /// other way costs a unique-violation exception that Entity Framework logs at
    /// error level with a stack trace — noise for an outcome that is not a fault.
    /// </summary>
    /// <remarks>
    /// Names must track the mapping on <see cref="AtmosCache" />. <c>create_at</c> is
    /// deliberately left alone on conflict: it records when the key was first cached,
    /// while <c>update_at</c> tracks the value being replaced.
    /// </remarks>
    private const string UpsertSql =
        """
        INSERT INTO atmos_cache
            (key, value, expire_at, absolute_expire_at, sliding_expiration_ticks, tags, create_at, update_at)
        VALUES (@key, @value, @expire_at, @absolute_expire_at, @sliding_expiration_ticks, @tags, @now, @now)
        ON CONFLICT (key) DO UPDATE SET
            value = EXCLUDED.value,
            expire_at = EXCLUDED.expire_at,
            absolute_expire_at = EXCLUDED.absolute_expire_at,
            sliding_expiration_ticks = EXCLUDED.sliding_expiration_ticks,
            tags = EXCLUDED.tags,
            update_at = EXCLUDED.update_at
        """;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;

    public AtmosCacheStore(IServiceScopeFactory scopeFactory, TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Reads an entry, extending it first when it has a sliding window.
    /// </summary>
    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);
        var now = _timeProvider.GetUtcNow();

        var entry = await LiveEntries(db, key, now).FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return null;
        }

        if (entry.SlidingExpirationTicks is { } ticks)
        {
            await SlideQuery(db, key, now, ticks, entry.AbsoluteExpireAt)
                .ExecuteUpdateAsync(Slide(now, ticks, entry.AbsoluteExpireAt), cancellationToken);
        }

        return entry.Value;
    }

    /// <inheritdoc cref="GetAsync" />
    public byte[]? Get(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);
        var now = _timeProvider.GetUtcNow();

        var entry = LiveEntries(db, key, now).FirstOrDefault();

        if (entry is null)
        {
            return null;
        }

        if (entry.SlidingExpirationTicks is { } ticks)
        {
            SlideQuery(db, key, now, ticks, entry.AbsoluteExpireAt)
                .ExecuteUpdate(Slide(now, ticks, entry.AbsoluteExpireAt));
        }

        return entry.Value;
    }

    /// <summary>
    /// Extends a sliding entry without reading its payload. A no-op for entries
    /// with no sliding window.
    /// </summary>
    public async ValueTask RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);
        var now = _timeProvider.GetUtcNow();

        var entry = await LiveExpiries(db, key, now).FirstOrDefaultAsync(cancellationToken);

        if (entry?.SlidingExpirationTicks is not { } ticks)
        {
            return;
        }

        await SlideQuery(db, key, now, ticks, entry.AbsoluteExpireAt)
            .ExecuteUpdateAsync(Slide(now, ticks, entry.AbsoluteExpireAt), cancellationToken);
    }

    /// <inheritdoc cref="RefreshAsync" />
    public void Refresh(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);
        var now = _timeProvider.GetUtcNow();

        var entry = LiveExpiries(db, key, now).FirstOrDefault();

        if (entry?.SlidingExpirationTicks is not { } ticks)
        {
            return;
        }

        SlideQuery(db, key, now, ticks, entry.AbsoluteExpireAt)
            .ExecuteUpdate(Slide(now, ticks, entry.AbsoluteExpireAt));
    }

    /// <summary>
    /// Inserts or overwrites an entry.
    /// </summary>
    public async ValueTask SetAsync(
        string key,
        byte[] value,
        AtmosCacheExpiration expiration,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);

        await db.Database.ExecuteSqlRawAsync(
            UpsertSql,
            UpsertParameters(key, value, expiration, tags ?? [], _timeProvider.GetUtcNow()),
            cancellationToken);
    }

    /// <inheritdoc cref="SetAsync" />
    public void Set(string key, byte[] value, AtmosCacheExpiration expiration, string[]? tags = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);

        db.Database.ExecuteSqlRaw(
            UpsertSql,
            UpsertParameters(key, value, expiration, tags ?? [], _timeProvider.GetUtcNow()));
    }

    /// <summary>
    /// Drops a single entry. Missing keys are not an error.
    /// </summary>
    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);

        await ByKey(db, key).ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc cref="RemoveAsync" />
    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);

        ByKey(db, key).ExecuteDelete();
    }

    /// <summary>
    /// Drops every entry carrying <paramref name="tag" />.
    /// </summary>
    public async ValueTask<int> EvictByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);

        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);

        return await db.AtmosCaches
            .Where(x => x.Tags.Contains(tag))
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes entries whose deadline has passed. Reads already filter these out,
    /// so this only reclaims space.
    /// </summary>
    public async ValueTask<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = GetDbContext(scope);
        var now = _timeProvider.GetUtcNow();

        return await db.AtmosCaches
            .Where(x => x.ExpireAt != null && x.ExpireAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static AtmosDbContext GetDbContext(IServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<AtmosDbContext>();
    }

    /// <summary>
    /// Parameters are built by hand because every nullable column would otherwise
    /// reach Npgsql as an untyped null, which it cannot bind.
    /// </summary>
    private static NpgsqlParameter[] UpsertParameters(
        string key,
        byte[] value,
        AtmosCacheExpiration expiration,
        string[] tags,
        DateTimeOffset now)
    {
        return
        [
            new NpgsqlParameter("key", NpgsqlDbType.Text) { Value = key },
            new NpgsqlParameter("value", NpgsqlDbType.Bytea) { Value = value },
            new NpgsqlParameter("expire_at", NpgsqlDbType.TimestampTz)
            {
                Value = (object?)expiration.ExpireAt ?? DBNull.Value
            },
            new NpgsqlParameter("absolute_expire_at", NpgsqlDbType.TimestampTz)
            {
                Value = (object?)expiration.AbsoluteExpireAt ?? DBNull.Value
            },
            new NpgsqlParameter("sliding_expiration_ticks", NpgsqlDbType.Bigint)
            {
                Value = (object?)expiration.SlidingExpiration?.Ticks ?? DBNull.Value
            },
            new NpgsqlParameter("tags", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = tags },
            new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now }
        ];
    }

    private static IQueryable<AtmosCache> ByKey(AtmosDbContext db, string key)
    {
        return db.AtmosCaches.Where(x => x.Key == key);
    }

    private static IQueryable<CacheEntry> LiveEntries(AtmosDbContext db, string key, DateTimeOffset now)
    {
        return db.AtmosCaches
            .AsNoTracking()
            .Where(x => x.Key == key && (x.ExpireAt == null || x.ExpireAt > now))
            .Select(x => new CacheEntry(x.Value, x.AbsoluteExpireAt, x.SlidingExpirationTicks));
    }

    private static IQueryable<CacheExpiry> LiveExpiries(AtmosDbContext db, string key, DateTimeOffset now)
    {
        return db.AtmosCaches
            .AsNoTracking()
            .Where(x => x.Key == key && (x.ExpireAt == null || x.ExpireAt > now))
            .Select(x => new CacheExpiry(x.AbsoluteExpireAt, x.SlidingExpirationTicks));
    }

    /// <summary>
    /// Narrows the update to the row we just read, so a concurrent overwrite that
    /// shortened the entry is not silently extended.
    /// </summary>
    private static IQueryable<AtmosCache> SlideQuery(
        AtmosDbContext db,
        string key,
        DateTimeOffset now,
        long slidingTicks,
        DateTimeOffset? absoluteExpireAt)
    {
        return db.AtmosCaches.Where(x =>
            x.Key == key &&
            (x.ExpireAt == null || x.ExpireAt > now) &&
            x.SlidingExpirationTicks == slidingTicks &&
            x.AbsoluteExpireAt == absoluteExpireAt);
    }

    /// <summary>
    /// The new deadline is computed in C# and written as a constant: <c>AddTicks</c>
    /// over a column has no PostgreSQL translation, and the read already gave us the
    /// row's window and hard deadline.
    /// </summary>
    private static Action<UpdateSettersBuilder<AtmosCache>> Slide(
        DateTimeOffset now,
        long slidingTicks,
        DateTimeOffset? absoluteExpireAt)
    {
        DateTimeOffset? expireAt = AtmosCacheExpiration.Slide(now, TimeSpan.FromTicks(slidingTicks), absoluteExpireAt);

        return setters =>
        {
            setters.SetProperty(x => x.ExpireAt, expireAt);
            setters.SetProperty(x => x.UpdateAt, now);
        };
    }

    private sealed record CacheEntry(byte[] Value, DateTimeOffset? AbsoluteExpireAt, long? SlidingExpirationTicks);

    private sealed record CacheExpiry(DateTimeOffset? AbsoluteExpireAt, long? SlidingExpirationTicks);
}
