using Atmos.Services.Media.Services;

namespace Atmos.Tests.Unit.Media;

public class MediaReferenceExtractorTests
{
    private const string BaseUrl = "https://atmos-resources.alisaqaq.moe";

    [Test]
    public async Task Extracts_Key_From_Markdown_Image()
    {
        var content = $"Intro\n\n![alt text]({BaseUrl}/blog/2026/photo.webp)\n";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys).IsEquivalentTo(new[] { "blog/2026/photo.webp" });
    }

    [Test]
    public async Task Extracts_Key_From_Markdown_Link()
    {
        var content = $"[download]({BaseUrl}/files/archive.zip)";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys).IsEquivalentTo(new[] { "files/archive.zip" });
    }

    [Test]
    public async Task Extracts_Key_From_Raw_Html_Img()
    {
        var content = $"""<img src="{BaseUrl}/blog/a.png" alt="" />""";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys).IsEquivalentTo(new[] { "blog/a.png" });
    }

    [Test]
    public async Task Extracts_Bare_Url_At_End_Of_Content()
    {
        var content = $"see {BaseUrl}/blog/a.png";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys).IsEquivalentTo(new[] { "blog/a.png" });
    }

    [Test]
    public async Task Duplicate_References_Are_Deduplicated()
    {
        var content = $"![]({BaseUrl}/a.png) and again ![]({BaseUrl}/a.png)";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Query_String_And_Fragment_Are_Stripped()
    {
        var content = $"![]({BaseUrl}/a.png?width=200) ![]({BaseUrl}/b.png#frag)";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys).IsEquivalentTo(new[] { "a.png", "b.png" });
    }

    [Test]
    public async Task Other_Hosts_Are_Ignored()
    {
        var content = "![](https://example.com/a.png)";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Invalid_Keys_Are_Ignored()
    {
        var content = $"![]({BaseUrl}/bad//double-slash.png)";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl);
        await Assert.That(keys.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Empty_Content_Returns_Empty_Set()
    {
        var keys = MediaReferenceExtractor.ExtractKeys(string.Empty, BaseUrl);
        await Assert.That(keys.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Base_Url_With_Trailing_Slash_Behaves_The_Same()
    {
        var content = $"![]({BaseUrl}/a.png)";
        var keys = MediaReferenceExtractor.ExtractKeys(content, BaseUrl + "/");
        await Assert.That(keys).IsEquivalentTo(new[] { "a.png" });
    }
}
