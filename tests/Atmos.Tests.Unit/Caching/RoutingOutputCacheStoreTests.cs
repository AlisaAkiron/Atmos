using System.Collections.Concurrent;
using Atmos.Services.Default.Caching;
using Microsoft.AspNetCore.OutputCaching;

namespace Atmos.Tests.Unit.Caching;

public class RoutingOutputCacheStoreTests
{
    private const string LocalKey = AtmosOutputCache.LocalKeyPrefix + "GETHTTPLOCALHOST/HEALTH";
    private const string SharedKey = "GETHTTPLOCALHOST/API/ARTICLES";

    [Test]
    public async Task Prefixed_Keys_Are_Written_To_The_Local_Store()
    {
        var (store, local, shared) = Create();

        await store.SetAsync(LocalKey, [1], null, TimeSpan.FromMinutes(1), CancellationToken.None);

        await Assert.That(local.Entries.ContainsKey(LocalKey)).IsTrue();
        await Assert.That(shared.Entries).IsEmpty();
    }

    [Test]
    public async Task Unprefixed_Keys_Are_Written_To_The_Shared_Store()
    {
        var (store, local, shared) = Create();

        await store.SetAsync(SharedKey, [2], null, TimeSpan.FromMinutes(1), CancellationToken.None);

        await Assert.That(shared.Entries.ContainsKey(SharedKey)).IsTrue();
        await Assert.That(local.Entries).IsEmpty();
    }

    [Test]
    public async Task Reads_Come_Back_From_The_Store_That_Was_Written()
    {
        var (store, _, _) = Create();

        await store.SetAsync(LocalKey, [1], null, TimeSpan.FromMinutes(1), CancellationToken.None);
        await store.SetAsync(SharedKey, [2], null, TimeSpan.FromMinutes(1), CancellationToken.None);

        await Assert.That(await store.GetAsync(LocalKey, CancellationToken.None)).IsEquivalentTo(new byte[] { 1 });
        await Assert.That(await store.GetAsync(SharedKey, CancellationToken.None)).IsEquivalentTo(new byte[] { 2 });
    }

    [Test]
    public async Task A_Miss_In_The_Routed_Store_Does_Not_Fall_Through_To_The_Other()
    {
        var (store, _, shared) = Create();

        await shared.SetAsync(LocalKey, [9], null, TimeSpan.FromMinutes(1), CancellationToken.None);

        await Assert.That(await store.GetAsync(LocalKey, CancellationToken.None)).IsNull();
    }

    [Test]
    public async Task Tag_Eviction_Sweeps_Both_Stores()
    {
        var (store, local, shared) = Create();

        await store.SetAsync(LocalKey, [1], ["articles"], TimeSpan.FromMinutes(1), CancellationToken.None);
        await store.SetAsync(SharedKey, [2], ["articles"], TimeSpan.FromMinutes(1), CancellationToken.None);

        await store.EvictByTagAsync("articles", CancellationToken.None);

        await Assert.That(local.EvictedTags).IsEquivalentTo(new[] { "articles" });
        await Assert.That(shared.EvictedTags).IsEquivalentTo(new[] { "articles" });
    }

    private static (RoutingOutputCacheStore Store, RecordingStore Local, RecordingStore Shared) Create()
    {
        var local = new RecordingStore();
        var shared = new RecordingStore();

        return (new RoutingOutputCacheStore(local, shared), local, shared);
    }

    private sealed class RecordingStore : IOutputCacheStore
    {
        public ConcurrentDictionary<string, byte[]> Entries { get; } = new(StringComparer.Ordinal);

        public List<string> EvictedTags { get; } = [];

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Entries.TryGetValue(key, out var value) ? value : null);
        }

        public ValueTask SetAsync(
            string key,
            byte[] value,
            string[]? tags,
            TimeSpan validFor,
            CancellationToken cancellationToken)
        {
            Entries[key] = value;

            return ValueTask.CompletedTask;
        }

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            EvictedTags.Add(tag);

            return ValueTask.CompletedTask;
        }
    }
}
