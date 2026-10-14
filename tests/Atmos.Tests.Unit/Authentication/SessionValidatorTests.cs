using Atmos.Domain.Entities.Identity;
using Atmos.Services.Api.Services;

namespace Atmos.Tests.Unit.Authentication;

public class SessionValidatorTests
{
    private static readonly Guid Stamp = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static User UserWith(Guid stamp, bool isSiteOwner = false)
    {
        return new User { UserId = Guid.NewGuid(), SecurityStamp = stamp, IsSiteOwner = isSiteOwner };
    }

    [Test]
    public async Task A_Matching_Stamp_Keeps_The_Session()
    {
        await Assert.That(SessionValidator.IsCurrent(UserWith(Stamp), Stamp, false)).IsTrue();
    }

    [Test]
    public async Task A_Deleted_Account_Ends_The_Session()
    {
        await Assert.That(SessionValidator.IsCurrent(null, Stamp, false)).IsFalse();
    }

    [Test]
    public async Task A_Bumped_Stamp_Ends_The_Session()
    {
        // What makes revoking a credential take effect: the stamp moves, every cookie
        // carrying the old one stops validating
        await Assert.That(SessionValidator.IsCurrent(UserWith(Guid.NewGuid()), Stamp, false)).IsFalse();
    }

    [Test]
    public async Task A_Cookie_Without_A_Stamp_Ends_The_Session()
    {
        // Issued before stamps existed, so it cannot be shown to be current
        await Assert.That(SessionValidator.IsCurrent(UserWith(Stamp), null, false)).IsFalse();
    }

    [Test]
    public async Task Losing_The_Site_Owner_Role_Ends_The_Session()
    {
        // is_site_owner is flipped by hand in the database, which cannot bump the stamp,
        // so the role is compared on its own
        await Assert.That(SessionValidator.IsCurrent(UserWith(Stamp, isSiteOwner: false), Stamp, true)).IsFalse();
    }

    [Test]
    public async Task Gaining_The_Site_Owner_Role_Ends_The_Session()
    {
        // The cookie carries no role claim, so the new privilege would go unused until
        // it expired; ending the session makes the next sign-in pick it up
        await Assert.That(SessionValidator.IsCurrent(UserWith(Stamp, isSiteOwner: true), Stamp, false)).IsFalse();
    }
}
