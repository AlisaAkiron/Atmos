using Atmos.Common.Utils;

namespace Atmos.Tests.Unit.Common;

public class MediaKeyUtilsTests
{
    [Test]
    [Arguments("photo.webp")]
    [Arguments("blog/2026/photo.webp")]
    [Arguments("friends/alice-avatar.png")]
    [Arguments("icons/GitHub_dark.svg")]
    [Arguments("a/b/c/d.bin")]
    public async Task Valid_Keys_Are_Accepted(string key)
    {
        await Assert.That(MediaKeyUtils.IsValid(key)).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments(null)]
    [Arguments("/leading-slash.png")]
    [Arguments("trailing-slash.png/")]
    [Arguments("double//slash.png")]
    [Arguments("dot-segment/../escape.png")]
    [Arguments(".hidden")]
    [Arguments("folder/.hidden")]
    [Arguments("space in name.png")]
    [Arguments("query?.png")]
    [Arguments("hash#.png")]
    [Arguments("日本語.png")]
    [Arguments("photo.png\n")]
    public async Task Invalid_Keys_Are_Rejected(string? key)
    {
        await Assert.That(MediaKeyUtils.IsValid(key)).IsFalse();
    }

    [Test]
    public async Task Key_Longer_Than_512_Chars_Is_Rejected()
    {
        var key = new string('a', 513);
        await Assert.That(MediaKeyUtils.IsValid(key)).IsFalse();
    }
}
