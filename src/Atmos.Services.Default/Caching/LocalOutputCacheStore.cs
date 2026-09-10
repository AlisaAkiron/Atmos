using System.Collections.Concurrent;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace Atmos.Services.Default.Caching;

/// <summary>
/// Process-local output cache store. Used for responses that are cheap to
/// regenerate and churn faster than a shared store is worth paying for — health
/// probes, which every instance is polled for independently.
/// </summary>
public sealed class LocalOutputCacheStore : IOutputCacheStore, IDisposable
{
    /// <summary>
    /// Total cached bytes. Entries here are small and short-lived; the limit only
    /// exists so a misrouted key cannot grow the heap without bound.
    /// </summary>
    private const long SizeLimitBytes = 4L * 1024 * 1024;

    private readonly MemoryCache _cache;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _keysByTag = new(StringComparer.Ordinal);

    public LocalOutputCacheStore()
    {
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = SizeLimitBytes });
    }

    /// <inheritdoc />
    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        return ValueTask.FromResult(_cache.Get<byte[]>(key));
    }

    /// <inheritdoc />
    public ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var entryTags = tags ?? [];

        foreach (var tag in entryTags)
        {
            TaggedKeys(tag)[key] = 0;
        }

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = validFor,

            // MemoryCache requires a size once SizeLimit is set. An empty body would
            // otherwise be weightless and never counted against the limit.
            Size = value.Length + 1
        };

        // Expiry and size eviction are invisible to the tag index, so it has to be
        // pruned from the callback or it grows for the lifetime of the process
        options.RegisterPostEvictionCallback(OnEvicted, entryTags);

        _cache.Set(key, value, options);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (_keysByTag.TryRemove(tag, out var keys))
        {
            foreach (var key in keys.Keys)
            {
                _cache.Remove(key);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cache.Dispose();
    }

    private ConcurrentDictionary<string, byte> TaggedKeys(string tag)
    {
        return _keysByTag.GetOrAdd(tag, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
    }

    private void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (key is not string cacheKey || state is not string[] tags)
        {
            return;
        }

        foreach (var tag in tags)
        {
            if (_keysByTag.TryGetValue(tag, out var keys))
            {
                keys.TryRemove(cacheKey, out _);
            }
        }
    }
}
