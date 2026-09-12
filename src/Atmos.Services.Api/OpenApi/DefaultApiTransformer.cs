using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

public class DefaultApiTransformer : IOpenApiDocumentTransformer
{
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
