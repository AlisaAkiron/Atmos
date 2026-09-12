using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

public class DefaultApiTransformer : IOpenApiDocumentTransformer
{
    /// <summary>
    /// Blurb for each tag the endpoint groups declare through WithTags.
    /// </summary>
    private static readonly Dictionary<string, string> TagDescriptions = new()
    {
        ["Authentication"] =
            "Signing in and out, discovering the configured identity providers, WebAuthn ceremonies, "
            + "and inspecting or revoking what can reach the caller's own account.",
        ["SocialLinks"] = "The site owner's profile links, as shown to visitors.",
        ["SocialLinks Admin"] = "Site-owner management of profile links.",
        ["FriendLinks"] = "Reciprocal links to other sites, as shown to visitors.",
        ["FriendLinks Admin"] = "Site-owner management of friend links.",
        ["Taxonomy"] = "Categories and tags that published content is filed under.",
        ["Taxonomy Admin"] = "Site-owner management of categories and tags.",
        ["Articles"] = "Published articles, newest first.",
        ["Articles Admin"] = "Site-owner authoring: drafts, edits, publishing and unpublishing.",
        ["Pages"] = "Published standalone pages, addressed by slug rather than listed.",
        ["Pages Admin"] = "Site-owner authoring of standalone pages.",
        ["Media Admin"] =
            "Site-owner upload and management of media. Delivery is public and served from the media "
            + "route directly, so it is not part of this document.",
        ["Debug"] = "Development-only diagnostics. These are not mapped in production.",
        ["Status"] = "Liveness and readiness probes. Unversioned, and outside the /api prefix."
    };

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Description =
            "HTTP API behind an Atmos site. Read endpoints are public; everything under /api/admin "
            + "requires a session cookie belonging to the site owner. Requests may pin a version with "
            + "the X-Atmos-Api-Version header, and default to version 1 when they do not.";

        var resp = new OpenApiResponses
        {
            ["200"] = new OpenApiResponse
            {
                Description = "System healthy",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["text/plain"] = new OpenApiMediaType
                    {
                        Example = "Health"
                    }
                }
            },
            ["503"] = new OpenApiResponse
            {
                Description = "System unhealthy",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["text/plain"] = new OpenApiMediaType
                    {
                        Example = "Unhealthy"
                    }
                }
            }
        };

        document.Tags ??= new HashSet<OpenApiTag>();
        document.Tags.Add(new OpenApiTag
        {
            Name = "Status"
        });

        // Every other tag is created from WithTags as operations are collected, so the
        // descriptions have to be attached to the tag objects already in the document
        foreach (var tag in document.Tags)
        {
            if (tag.Name is not null && TagDescriptions.TryGetValue(tag.Name, out var description))
            {
                tag.Description = description;
            }
        }

        var tagRef = new HashSet<OpenApiTagReference>
        {
            new("Status")
        };

        document.Paths.Add("/health", new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new()
                {
                    Tags = tagRef,
                    Summary = "Readiness probe",
                    Description = "Get to know if the system can serve requests.",
                    Responses = resp
                }
            }
        });

        document.Paths.Add("/alive", new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation>
            {
                [HttpMethod.Get] = new()
                {
                    Tags = tagRef,
                    Summary = "Liveness probe",
                    Description = "Get to know if the system is running.",
                    Responses = resp
                }
            }
        });

        return Task.CompletedTask;
    }
}
