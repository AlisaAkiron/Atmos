using Atmos.Services.Media.Options;
using Atmos.Services.Media.Services;

namespace Atmos.Tests.Unit.Media;

public class MediaUrlBuilderTests
{
    private static MediaUrlBuilder Create(string publicBaseUrl)
    {
        return new MediaUrlBuilder(Microsoft.Extensions.Options.Options.Create(new MediaOptions
        {
            PublicBaseUrl = publicBaseUrl
        }));
    }

    [Test]
    public async Task Builds_Public_Url_From_Key()
    {
        var builder = Create("https://atmos-resources.alisaqaq.moe");
        await Assert.That(builder.GetPublicUrl("blog/2026/photo.webp"))
            .IsEqualTo("https://atmos-resources.alisaqaq.moe/blog/2026/photo.webp");
    }

    [Test]
    public async Task Trailing_Slash_On_Base_Url_Is_Normalized()
    {
        var builder = Create("https://atmos-resources.alisaqaq.moe/");
        await Assert.That(builder.GetPublicUrl("a.png"))
            .IsEqualTo("https://atmos-resources.alisaqaq.moe/a.png");
    }
}
