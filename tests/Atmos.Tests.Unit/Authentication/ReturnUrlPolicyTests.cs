using Atmos.Services.Api.Services;

namespace Atmos.Tests.Unit.Authentication;

public class ReturnUrlPolicyTests
{
    private static readonly string[] Allowed = ["https://app.example.test"];

    [Test]
    [Arguments("/")]
    [Arguments("/admin/articles")]
    [Arguments("/admin?tab=drafts")]
    public async Task Local_Paths_Are_Allowed(string returnUrl)
    {
        // Still the same-origin case, where the frontend is proxied onto the API's origin
        await Assert.That(ReturnUrlPolicy.IsAllowed(returnUrl, Allowed)).IsTrue();
    }

    [Test]
    [Arguments("https://app.example.test")]
    [Arguments("https://app.example.test/")]
    [Arguments("https://app.example.test/admin/articles?tab=drafts#top")]
    public async Task An_Allowlisted_Origin_Is_Allowed_With_Any_Path(string returnUrl)
    {
        await Assert.That(ReturnUrlPolicy.IsAllowed(returnUrl, Allowed)).IsTrue();
    }

    [Test]
    public async Task An_Unlisted_Origin_Is_Refused()
    {
        await Assert.That(ReturnUrlPolicy.IsAllowed("https://evil.test/", Allowed)).IsFalse();
    }

    [Test]
    [Arguments("//evil.test")]
    [Arguments("//evil.test/admin")]
    [Arguments(@"/\evil.test")]
    public async Task Protocol_Relative_Urls_Are_Refused(string returnUrl)
    {
        // Starts with a slash but leaves the origin: browsers read these as absolute
        await Assert.That(ReturnUrlPolicy.IsAllowed(returnUrl, Allowed)).IsFalse();
    }

    [Test]
    public async Task A_Host_That_Merely_Ends_With_An_Allowlisted_One_Is_Refused()
    {
        // The prefix-match bug: "https://app.example.test" is a prefix of this
        await Assert.That(ReturnUrlPolicy.IsAllowed("https://app.example.test.evil.test/", Allowed)).IsFalse();
    }

    [Test]
    public async Task An_Allowlisted_Host_In_The_Userinfo_Is_Refused()
    {
        // Browsers navigate to evil.test here; the allowlisted name is only a username
        await Assert.That(ReturnUrlPolicy.IsAllowed("https://app.example.test@evil.test/", Allowed)).IsFalse();
    }

    [Test]
    public async Task The_Scheme_Must_Match()
    {
        await Assert.That(ReturnUrlPolicy.IsAllowed("http://app.example.test/", Allowed)).IsFalse();
    }

    [Test]
    public async Task The_Port_Must_Match()
    {
        await Assert.That(ReturnUrlPolicy.IsAllowed("https://app.example.test:8443/", Allowed)).IsFalse();
    }

    [Test]
    [Arguments("javascript:alert(1)")]
    [Arguments("data:text/html,<script>alert(1)</script>")]
    public async Task Non_Http_Schemes_Are_Refused(string returnUrl)
    {
        await Assert.That(ReturnUrlPolicy.IsAllowed(returnUrl, Allowed)).IsFalse();
    }

    [Test]
    public async Task A_Malformed_Allowlist_Entry_Matches_Nothing()
    {
        await Assert.That(ReturnUrlPolicy.IsAllowed("https://evil.test/", ["not-a-url"])).IsFalse();
    }

    [Test]
    public async Task An_Empty_Allowlist_Permits_Only_Local_Paths()
    {
        // The default, and today's behaviour: nothing changes until ReturnUrls is set
        await Assert.That(ReturnUrlPolicy.IsAllowed("/admin", [])).IsTrue();
        await Assert.That(ReturnUrlPolicy.IsAllowed("https://app.example.test/", [])).IsFalse();
    }
}
