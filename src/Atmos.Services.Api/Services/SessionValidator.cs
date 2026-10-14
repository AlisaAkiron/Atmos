using Atmos.Domain.Entities.Identity;

namespace Atmos.Services.Api.Services;

/// <summary>
/// Decides whether a session cookie still describes the account behind it.
/// </summary>
/// <remarks>
/// The cookie is self-contained: once issued it carries its claims for the full 14
/// days regardless of what changes in the database. This is what re-checks it, so
/// revoking a credential or a role takes effect on the next request rather than on
/// expiry.
/// </remarks>
public static class SessionValidator
{
    /// <param name="user">The account as it stands now, or null if it is gone.</param>
    /// <param name="securityStamp">Stamp carried by the cookie.</param>
    /// <param name="isSiteOwner">Whether the cookie carries the site owner role.</param>
    public static bool IsCurrent(User? user, Guid? securityStamp, bool isSiteOwner)
    {
        if (user is null || securityStamp is null)
        {
            return false;
        }

        // Compared as well as the stamp: is_site_owner is flipped by hand in the
        // database, which cannot bump a stamp. Either direction ends the session -
        // losing the role to drop the privilege, gaining it so the next sign-in
        // picks up a claim this cookie will never carry.
        return securityStamp == user.SecurityStamp && isSiteOwner == user.IsSiteOwner;
    }
}
