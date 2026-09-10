using Atmos.Database.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Atmos.Tests.Unit.Caching;

public class AtmosCacheExpirationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Empty_Options_Never_Expire()
    {
        var expiration = AtmosCacheExpiration.Resolve(new DistributedCacheEntryOptions(), Now);

        await Assert.That(expiration).IsEqualTo(AtmosCacheExpiration.Never);
    }

    [Test]
    public async Task Relative_Absolute_Expiration_Is_Anchored_To_Now()
    {
        var expiration = AtmosCacheExpiration.Resolve(
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            Now);

        await Assert.That(expiration.ExpireAt).IsEqualTo(Now.AddMinutes(5));
        await Assert.That(expiration.AbsoluteExpireAt).IsEqualTo(Now.AddMinutes(5));
        await Assert.That(expiration.SlidingExpiration).IsNull();
    }

    [Test]
    public async Task Relative_Absolute_Expiration_Wins_Over_Fixed()
    {
        var expiration = AtmosCacheExpiration.Resolve(
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = Now.AddHours(1),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            Now);

        await Assert.That(expiration.AbsoluteExpireAt).IsEqualTo(Now.AddMinutes(5));
    }

    [Test]
    public async Task Absolute_Expiration_In_The_Past_Is_Rejected()
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpiration = Now.AddSeconds(-1) };

        await Assert.That(() => AtmosCacheExpiration.Resolve(options, Now))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Sliding_Expiration_Alone_Has_No_Hard_Deadline()
    {
        var expiration = AtmosCacheExpiration.Resolve(
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(2) },
            Now);

        await Assert.That(expiration.ExpireAt).IsEqualTo(Now.AddMinutes(2));
        await Assert.That(expiration.AbsoluteExpireAt).IsNull();
        await Assert.That(expiration.SlidingExpiration).IsEqualTo(TimeSpan.FromMinutes(2));
    }

    [Test]
    public async Task Sliding_Window_Is_Clamped_To_The_Absolute_Deadline()
    {
        var expiration = AtmosCacheExpiration.Resolve(
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            Now);

        await Assert.That(expiration.ExpireAt).IsEqualTo(Now.AddMinutes(5));
    }

    [Test]
    public async Task Slide_Extends_From_Now()
    {
        var next = AtmosCacheExpiration.Slide(Now, TimeSpan.FromMinutes(10), null);

        await Assert.That(next).IsEqualTo(Now.AddMinutes(10));
    }

    [Test]
    public async Task Slide_Never_Passes_The_Absolute_Deadline()
    {
        var cap = Now.AddMinutes(3);

        var next = AtmosCacheExpiration.Slide(Now, TimeSpan.FromMinutes(10), cap);

        await Assert.That(next).IsEqualTo(cap);
    }

    [Test]
    public async Task Oversized_Window_Saturates_Instead_Of_Overflowing()
    {
        var expiration = AtmosCacheExpiration.Resolve(
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.MaxValue },
            Now);

        await Assert.That(expiration.ExpireAt).IsEqualTo(DateTimeOffset.MaxValue);
    }

    [Test]
    public async Task Local_Deadlines_Are_Normalised_To_Utc()
    {
        // PostgreSQL's timestamptz only binds offset 0, and callers reach for
        // DateTimeOffset.Now without thinking about it
        // 13:00 UTC, an hour after Now
        var local = new DateTimeOffset(2026, 7, 30, 21, 0, 0, TimeSpan.FromHours(8));

        var expiration = AtmosCacheExpiration.Resolve(
            new DistributedCacheEntryOptions { AbsoluteExpiration = local },
            Now);

        await Assert.That(expiration.ExpireAt!.Value.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(expiration.ExpireAt).IsEqualTo(local);
    }

    [Test]
    public async Task Until_Normalises_To_Utc()
    {
        // 13:00 UTC, an hour after Now
        var local = new DateTimeOffset(2026, 7, 30, 21, 0, 0, TimeSpan.FromHours(8));

        var expiration = AtmosCacheExpiration.Until(local);

        await Assert.That(expiration.ExpireAt!.Value.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(expiration.AbsoluteExpireAt!.Value.Offset).IsEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task Until_Describes_A_Fixed_Deadline()
    {
        var deadline = Now.AddMinutes(1);

        var expiration = AtmosCacheExpiration.Until(deadline);

        await Assert.That(expiration.ExpireAt).IsEqualTo(deadline);
        await Assert.That(expiration.AbsoluteExpireAt).IsEqualTo(deadline);
        await Assert.That(expiration.SlidingExpiration).IsNull();
    }
}
