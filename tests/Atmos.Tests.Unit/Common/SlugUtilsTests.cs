using Atmos.Common.Utils;

namespace Atmos.Tests.Unit.Common;

public class SlugUtilsTests
{
    [Test]
    [Arguments("about")]
    [Arguments("hello-world")]
    [Arguments("post-2026-01")]
    [Arguments("a")]
    [Arguments("1")]
    public async Task Valid_Slugs_Are_Accepted(string slug)
    {
        await Assert.That(SlugUtils.IsValid(slug)).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments(null)]
    [Arguments("Hello")]
    [Arguments("hello world")]
    [Arguments("-leading")]
    [Arguments("trailing-")]
    [Arguments("double--dash")]
    [Arguments("under_score")]
    [Arguments("日本語")]
    public async Task Invalid_Slugs_Are_Rejected(string? slug)
    {
        await Assert.That(SlugUtils.IsValid(slug)).IsFalse();
    }

    [Test]
    public async Task Slug_Longer_Than_128_Chars_Is_Rejected()
    {
        var slug = new string('a', 129);
        await Assert.That(SlugUtils.IsValid(slug)).IsFalse();
    }
}
