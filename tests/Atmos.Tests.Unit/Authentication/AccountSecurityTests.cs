using Atmos.Services.Api.Services;

namespace Atmos.Tests.Unit.Authentication;

public class AccountSecurityTests
{
    [Test]
    public async Task Last_Social_Login_Cannot_Be_Unlinked()
    {
        // Accounts are only created by an external provider, so removing the last
        // one would leave an account nothing can sign into
        await Assert.That(AccountSecurity.CanUnlinkSocialLogin(1)).IsFalse();
    }

    [Test]
    public async Task Social_Login_Can_Be_Unlinked_When_Another_Remains()
    {
        await Assert.That(AccountSecurity.CanUnlinkSocialLogin(2)).IsTrue();
    }

    [Test]
    public async Task No_Social_Logins_Leaves_Nothing_To_Unlink()
    {
        await Assert.That(AccountSecurity.CanUnlinkSocialLogin(0)).IsFalse();
    }

    [Test]
    public async Task WebAuthn_Credentials_Are_Always_Revocable()
    {
        // The external login is always still there, so revoking a lost authenticator
        // must never be refused
        await Assert.That(AccountSecurity.CanRemoveWebAuthnCredential()).IsTrue();
    }
}
