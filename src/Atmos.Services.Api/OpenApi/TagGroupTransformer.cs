using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

/// <summary>
/// Owns how tags present. Operations are tagged per resource and audience, which leaves the
/// sidebar a flat list of "X" and "X Admin" pairs; Scalar can nest that into sections given
/// x-tagGroups, and show a cleaner label per tag given x-displayName.
/// </summary>
public class TagGroupTransformer : IOpenApiDocumentTransformer
{
    private const string FallbackGroup = "Other";

    /// <param name="Name">Tag as the endpoints declare it through WithTags.</param>
    /// <param name="Group">Sidebar section it belongs in.</param>
    /// <param name="DisplayName">Label shown in place of the tag name.</param>
    /// <param name="Description">Blurb shown under the label.</param>
    private sealed record TagPresentation(string Name, string Group, string DisplayName, string Description);

    // Declaration order is the order tags appear within their group
    private static readonly TagPresentation[] Presentations =
    [
        new("Articles", "Public", "Articles",
            "Published articles, newest first."),
        new("Pages", "Public", "Pages",
            "Published standalone pages, addressed by slug rather than listed."),
        new("Taxonomy", "Public", "Categories & Tags",
            "Categories and tags that published content is filed under."),
        new("SocialLinks", "Public", "Social Links",
            "The site owner's profile links, as shown to visitors."),
        new("FriendLinks", "Public", "Friend Links",
            "Reciprocal links to other sites, as shown to visitors."),

        new("Articles Admin", "Administration", "Articles",
            "Authoring: drafts, edits, publishing and unpublishing."),
        new("Pages Admin", "Administration", "Pages",
            "Authoring of standalone pages."),
        new("Taxonomy Admin", "Administration", "Categories & Tags",
            "Managing the categories and tags content is filed under."),
        new("SocialLinks Admin", "Administration", "Social Links",
            "Managing profile links."),
        new("FriendLinks Admin", "Administration", "Friend Links",
            "Managing friend links."),
        new("Media Admin", "Administration", "Media",
            "Upload and management of media. Delivery is public and served from the media route "
            + "directly, so it is not part of this document."),

        new("Authentication", "Account", "Authentication",
            "Signing in and out, discovering the configured identity providers, WebAuthn ceremonies, "
            + "and inspecting or revoking what can reach the caller's own account."),

        new("Status", "Operations", "Status",
            "Liveness and readiness probes. Unversioned, and outside the /api prefix."),
        new("Debug", "Operations", "Debug",
            "Development-only diagnostics. These are not mapped in production.")
    ];

    private static readonly string[] GroupOrder =
        [.. Presentations.Select(presentation => presentation.Group).Distinct(), FallbackGroup];

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Tags ??= new HashSet<OpenApiTag>();

        var byName = Presentations.ToDictionary(presentation => presentation.Name, StringComparer.Ordinal);

        foreach (var tag in document.Tags)
        {
            if (tag.Name is null || byName.TryGetValue(tag.Name, out var presentation) is false)
            {
                continue;
            }

            tag.Description = presentation.Description;
            tag.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
            tag.Extensions["x-displayName"] = new JsonNodeExtension(presentation.DisplayName);
        }

        // Scalar renders only what a group names, so a tag left out of every group disappears
        // from the sidebar. Anything unrecognised goes to the fallback group rather than vanishing.
        var present = document.Tags
            .Select(tag => tag.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var grouped = Presentations
            .Where(presentation => present.Contains(presentation.Name))
            .Select(presentation => (presentation.Group, presentation.Name))
            .Concat(present
                .Where(name => byName.ContainsKey(name) is false)
                .Order(StringComparer.Ordinal)
                .Select(name => (Group: FallbackGroup, Name: name)))
            .ToLookup(entry => entry.Group, entry => entry.Name, StringComparer.Ordinal);

        var groups = new JsonArray();

        foreach (var group in GroupOrder)
        {
            var names = grouped[group].ToList();
            if (names.Count == 0)
            {
                continue;
            }

            groups.Add(new JsonObject
            {
                ["name"] = group,
                ["tags"] = new JsonArray([.. names.Select(name => (JsonNode)JsonValue.Create(name))])
            });
        }

        document.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
        document.Extensions["x-tagGroups"] = new JsonNodeExtension(groups);

        return Task.CompletedTask;
    }
}
