using Atmos.Services.Default.Caching;

namespace Atmos.Tests.Unit.Caching;

public class LocalOutputCacheStoreTests
{
    private static readonly TimeSpan ValidFor = TimeSpan.FromMinutes(5);

    [Test]
    public async Task Stored_Responses_Are_Read_Back()
    {
        using var store = new LocalOutputCacheStore();

        await store.SetAsync("a", [1, 2, 3], null, ValidFor, CancellationToken.None);

        await Assert.That(await store.GetAsync("a", CancellationToken.None)).IsEquivalentTo(new byte[] { 1, 2, 3 });
    }

    [Test]
    public async Task Unknown_Keys_Are_A_Miss()
    {
        using var store = new LocalOutputCacheStore();

        await Assert.That(await store.GetAsync("missing", CancellationToken.None)).IsNull();
    }

    [Test]
    public async Task An_Empty_Body_Is_Still_Cached()
    {
        using var store = new LocalOutputCacheStore();

        await store.SetAsync("empty", [], null, ValidFor, CancellationToken.None);

        await Assert.That(await store.GetAsync("empty", CancellationToken.None)).IsNotNull();
    }

    [Test]
    public async Task Tag_Eviction_Only_Drops_Entries_Carrying_The_Tag()
    {
        using var store = new LocalOutputCacheStore();

        await store.SetAsync("tagged", [1], ["articles"], ValidFor, CancellationToken.None);
        await store.SetAsync("other", [2], ["pages"], ValidFor, CancellationToken.None);
        await store.SetAsync("untagged", [3], null, ValidFor, CancellationToken.None);

        await store.EvictByTagAsync("articles", CancellationToken.None);

        await Assert.That(await store.GetAsync("tagged", CancellationToken.None)).IsNull();
        await Assert.That(await store.GetAsync("other", CancellationToken.None)).IsNotNull();
        await Assert.That(await store.GetAsync("untagged", CancellationToken.None)).IsNotNull();
    }

    [Test]
    public async Task An_Entry_Is_Evictable_By_Any_Of_Its_Tags()
    {
        using var store = new LocalOutputCacheStore();

        await store.SetAsync("multi", [1], ["articles", "taxonomy"], ValidFor, CancellationToken.None);

        await store.EvictByTagAsync("taxonomy", CancellationToken.None);

        await Assert.That(await store.GetAsync("multi", CancellationToken.None)).IsNull();
    }

    [Test]
    public async Task Evicting_A_Tag_Twice_Is_Harmless()
    {
        using var store = new LocalOutputCacheStore();

        await store.SetAsync("tagged", [1], ["articles"], ValidFor, CancellationToken.None);

        await store.EvictByTagAsync("articles", CancellationToken.None);
        await store.EvictByTagAsync("articles", CancellationToken.None);

        await Assert.That(await store.GetAsync("tagged", CancellationToken.None)).IsNull();
    }

    [Test]
    public async Task Overwriting_A_Key_Replaces_Its_Value()
    {
        using var store = new LocalOutputCacheStore();

        await store.SetAsync("a", [1], null, ValidFor, CancellationToken.None);
        await store.SetAsync("a", [2], null, ValidFor, CancellationToken.None);

        await Assert.That(await store.GetAsync("a", CancellationToken.None)).IsEquivalentTo(new byte[] { 2 });
    }
}
