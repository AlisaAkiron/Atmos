namespace Atmos.Services.Api.Services;

/// <summary>
/// Rules that keep credential management from locking an account out of itself.
/// </summary>
public static class AccountSecurity
{
    /// <summary>
    /// Whether an external login may be unlinked.
    /// </summary>
    /// <remarks>
    /// An account is only ever created by an external identity provider, and WebAuthn
    /// registers credentials onto an account that already exists. Removing the last
    /// external login would therefore leave an account no one can sign into and no
    /// flow can recover, so the last one is held back.
    /// </remarks>
    /// <param name="linkedSocialLoginCount">External logins currently linked to the account.</param>
    public static bool CanUnlinkSocialLogin(int linkedSocialLoginCount)
    {
        return linkedSocialLoginCount > 1;
    }

    /// <summary>
    /// Whether a WebAuthn credential may be removed.
    /// </summary>
    /// <remarks>
    /// Always allowed: the external login remains as a way back in, and refusing to
    /// revoke the last authenticator would be worse than the lockout it avoids.
    /// </remarks>
    public static bool CanRemoveWebAuthnCredential()
    {
        return true;
    }
}
