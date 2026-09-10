using Microsoft.Extensions.Caching.Distributed;

namespace Atmos.Database.Caching;

/// <summary>
/// Expiry of a cache entry, resolved against a fixed "now" so the same instant is
/// used for every field of a single write.
/// </summary>
/// <param name="ExpireAt">
/// When the entry stops being served, or <see langword="null"/> to never expire.
/// </param>
/// <param name="AbsoluteExpireAt">
/// Hard deadline a sliding refresh can never push <paramref name="ExpireAt"/> past.
/// </param>
/// <param name="SlidingExpiration">Sliding window, or <see langword="null"/> if reads do not extend the entry.</param>
public readonly record struct AtmosCacheExpiration(
    DateTimeOffset? ExpireAt,
    DateTimeOffset? AbsoluteExpireAt,
    TimeSpan? SlidingExpiration)
{
    /// <summary>
    /// An entry that only goes away when it is explicitly removed or evicted.
    /// </summary>
    public static AtmosCacheExpiration Never => new(null, null, null);

    /// <summary>
    /// A fixed-deadline entry, which is all the output cache ever needs.
    /// </summary>
    public static AtmosCacheExpiration Until(DateTimeOffset expireAt)
    {
        var utc = expireAt.ToUniversalTime();

        return new AtmosCacheExpiration(utc, utc, null);
    }

    /// <summary>
    /// Translates <see cref="DistributedCacheEntryOptions"/> into stored expiry columns.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="DistributedCacheEntryOptions.AbsoluteExpiration"/> is not in the future.
    /// </exception>
    public static AtmosCacheExpiration Resolve(DistributedCacheEntryOptions options, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Every stored deadline is an instant in UTC. Callers routinely hand over a
        // local DateTimeOffset, and PostgreSQL's timestamptz only accepts offset 0.
        now = now.ToUniversalTime();

        var absolute = options.AbsoluteExpiration?.ToUniversalTime();

        if (absolute is { } deadline && deadline <= now)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                deadline,
                "The absolute expiration value must be in the future.");
        }

        // Both set: relative wins, matching SqlServerCache and MemoryDistributedCache
        if (options.AbsoluteExpirationRelativeToNow is { } relative)
        {
            absolute = Add(now, relative);
        }

        // The DistributedCacheEntryOptions setters already reject non-positive windows
        var sliding = options.SlidingExpiration;

        var expireAt = sliding is { } window
            ? Slide(now, window, absolute)
            : absolute;

        return new AtmosCacheExpiration(expireAt, absolute, sliding);
    }

    /// <summary>
    /// Pushes a sliding entry's deadline out from <paramref name="now"/>, clamped to
    /// <paramref name="absoluteExpireAt"/>.
    /// </summary>
    public static DateTimeOffset Slide(DateTimeOffset now, TimeSpan slidingExpiration, DateTimeOffset? absoluteExpireAt)
    {
        var next = Add(now, slidingExpiration);

        return absoluteExpireAt is { } cap && next > cap ? cap : next;
    }

    /// <summary>
    /// Saturating add: callers may pass windows large enough to overflow, and a cache
    /// entry that lives until <see cref="DateTimeOffset.MaxValue"/> is the intent anyway.
    /// </summary>
    private static DateTimeOffset Add(DateTimeOffset value, TimeSpan offset)
    {
        return offset >= DateTimeOffset.MaxValue - value
            ? DateTimeOffset.MaxValue
            : value.Add(offset);
    }
}
