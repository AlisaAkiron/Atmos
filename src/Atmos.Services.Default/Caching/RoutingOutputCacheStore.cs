using Microsoft.AspNetCore.OutputCaching;

namespace Atmos.Services.Default.Caching;

/// <summary>
/// Sends each cache key to one of two stores, so a single app can keep cheap,
/// high-churn responses in process while everything else goes to a shared store.
/// </summary>
/// <remarks>
/// The output cache middleware resolves exactly one <see cref="IOutputCacheStore" />,
/// so per-policy backing stores have to be expressed in the key itself. Policies opt
/// into the local store with
/// <see cref="AtmosOutputCache.LocalKeyPrefix" />.
/// </remarks>
public sealed class RoutingOutputCacheStore : IOutputCacheStore
{
    private readonly IOutputCacheStore _local;
    private readonly IOutputCacheStore _shared;

    public RoutingOutputCacheStore(IOutputCacheStore local, IOutputCacheStore shared)
    {
        _local = local;
        _shared = shared;
    }

    /// <inheritdoc />
    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return Route(key).GetAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        return Route(key).SetAsync(key, value, tags, validFor, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        // Tags carry no routing information, so both stores have to be swept
        await _local.EvictByTagAsync(tag, cancellationToken);
        await _shared.EvictByTagAsync(tag, cancellationToken);
    }

    private IOutputCacheStore Route(string key)
    {
        return key.StartsWith(AtmosOutputCache.LocalKeyPrefix, StringComparison.Ordinal) ? _local : _shared;
    }
}
