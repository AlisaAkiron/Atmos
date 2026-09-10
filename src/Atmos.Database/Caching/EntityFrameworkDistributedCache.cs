using Microsoft.Extensions.Caching.Distributed;

namespace Atmos.Database.Caching;

/// <summary>
/// <see cref="IDistributedCache" /> backed by the <c>atmos_cache</c> table, so the
/// deployment needs no cache server of its own.
/// </summary>
/// <remarks>
/// Failures propagate. Callers such as the WebAuthn challenge flow depend on a write
/// having actually landed, and silently losing an entry there would look like a
/// replay-protection bug rather than an outage.
/// </remarks>
public sealed class EntityFrameworkDistributedCache : IDistributedCache
{
    private readonly AtmosCacheStore _store;
    private readonly TimeProvider _timeProvider;

    public EntityFrameworkDistributedCache(AtmosCacheStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public byte[]? Get(string key)
    {
        return _store.Get(key);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return await _store.GetAsync(key, token);
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _store.Set(key, value, AtmosCacheExpiration.Resolve(options, _timeProvider.GetUtcNow()));
    }

    /// <inheritdoc />
    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        var expiration = AtmosCacheExpiration.Resolve(options, _timeProvider.GetUtcNow());

        await _store.SetAsync(key, value, expiration, cancellationToken: token);
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        _store.Refresh(key);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        await _store.RefreshAsync(key, token);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _store.Remove(key);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await _store.RemoveAsync(key, token);
    }
}
