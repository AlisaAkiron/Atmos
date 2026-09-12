using System.Text.Json.Nodes;
using Atmos.Services.Api.OpenApi;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Atmos.Tests.Unit.OpenApi;

public class TagGroupTransformerTests
{
    private static OpenApiDocument Transform(params string[] tagNames)
    {
        var document = new OpenApiDocument
        {
            Tags = new HashSet<OpenApiTag>(tagNames.Select(name => new OpenApiTag { Name = name }))
        };

        var context = new OpenApiDocumentTransformerContext
        {
            DocumentName = "api",
            DescriptionGroups = [],
            ApplicationServices = new ServiceCollection().BuildServiceProvider()
        };

        new TagGroupTransformer()
            .TransformAsync(document, context, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return document;
    }

    private static Dictionary<string, List<string>> GroupsOf(OpenApiDocument document)
    {
        var extension = (JsonNodeExtension)document.Extensions!["x-tagGroups"];

        return ((JsonArray)extension.Node)
            .Select(node => node!.AsObject())
            .ToDictionary(
                group => (string)group["name"]!,
                group => ((JsonArray)group["tags"]!).Select(tag => (string)tag!).ToList());
    }

    [Test]
    public async Task Splits_The_Public_And_Admin_Halves_Of_A_Resource_Across_Groups()
    {
        var groups = GroupsOf(Transform("Articles", "Articles Admin"));

        await Assert.That(groups["Public"]).IsEquivalentTo(new[] { "Articles" });
        await Assert.That(groups["Administration"]).IsEquivalentTo(new[] { "Articles Admin" });
    }

    [Test]
    public async Task Drops_The_Admin_Suffix_From_The_Displayed_Label()
    {
        var document = Transform("Articles Admin", "SocialLinks", "Media Admin", "Taxonomy");

        var displayed = document.Tags!.ToDictionary(
            tag => tag.Name!,
            tag => (string)((JsonNodeExtension)tag.Extensions!["x-displayName"]).Node!);

        await Assert.That(displayed["Articles Admin"]).IsEqualTo("Articles");
        await Assert.That(displayed["Media Admin"]).IsEqualTo("Media");
        await Assert.That(displayed["SocialLinks"]).IsEqualTo("Social Links");
        await Assert.That(displayed["Taxonomy"]).IsEqualTo("Categories & Tags");
    }

    [Test]
    public async Task Names_No_Group_Tag_That_Is_Absent_From_The_Document()
    {
        // Only Debug is mapped outside production, so a group must never promise it otherwise
        var document = Transform("Articles", "Status");
        var grouped = GroupsOf(document).Values.SelectMany(tags => tags).ToList();

        await Assert.That(grouped).IsEquivalentTo(new[] { "Articles", "Status" });
    }

    [Test]
    public async Task Keeps_An_Unrecognised_Tag_Visible_Rather_Than_Dropping_It()
    {
        // A tag left out of every group vanishes from the sidebar entirely, so a new
        // WithTags value has to surface somewhere until it is given a home
        var groups = GroupsOf(Transform("Articles", "Newly Added"));

        await Assert.That(groups["Other"]).IsEquivalentTo(new[] { "Newly Added" });
    }

    [Test]
    public async Task Orders_Groups_So_Public_Comes_First()
    {
        var groups = GroupsOf(Transform("Status", "Articles Admin", "Authentication", "Articles"));

        await Assert.That(groups.Keys.ToList())
            .IsEquivalentTo(new[] { "Public", "Administration", "Account", "Operations" });
    }
}
