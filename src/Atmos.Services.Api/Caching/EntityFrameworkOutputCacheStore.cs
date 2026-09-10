using Atmos.Database.Caching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace Atmos.Services.Api.Caching;

/// <summary>
/// Output cache store backed by the <c>atmos_cache</c> table, so cached responses
/// survive restarts and every instance evicts the same entries.
/// </summary>
public sealed class EntityFrameworkOutputCacheStore : IOutputCacheStore
{
    /// <summary>
    /// Namespaces output cache keys inside the shared table, so a tag eviction can
    /// never reach an <c>IDistributedCache</c> entry.
    /// </summary>
    private const string KeyPrefix = "output-cache:";

    private readonly AtmosCacheStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EntityFrameworkOutputCacheStore> _logger;

    public EntityFrameworkOutputCacheStore(
        AtmosCacheStore store,
        TimeProvider timeProvider,
        ILogger<EntityFrameworkOutputCacheStore> logger)
    {
        _store = store;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A read failure degrades to a cache miss. The response is still correct, just
    /// more expensive, which beats failing the request over an optimisation.
    /// </remarks>
    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            return await _store.GetAsync(KeyPrefix + key, cancellationToken);
        }
        catch (Exception ex) when (NotCancellation(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Output cache read failed for {Key}; treating as a miss", key);

            return null;
        }
    }

    /// <inheritdoc />
    /// <remarks>A writing failure degrades to not caching this response.</remarks>
    public async ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var expiration = AtmosCacheExpiration.Until(_timeProvider.GetUtcNow().Add(validFor));

        try
        {
            await _store.SetAsync(KeyPrefix + key, value, expiration, tags, cancellationToken);
        }
        catch (Exception ex) when (NotCancellation(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Output cache write failed for {Key}; response was not cached", key);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Failures propagate here, unlike reads and writes. Eviction is what keeps
    /// published content from going stale after an edit, and swallowing it would
    /// leave the wrong response served for the rest of the entry's lifetime.
    /// </remarks>
    public async ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tag);

        await _store.EvictByTagAsync(tag, cancellationToken);
    }

    private static bool NotCancellation(Exception exception, CancellationToken cancellationToken)
    {
        return exception is not OperationCanceledException || cancellationToken.IsCancellationRequested is false;
    }
}
