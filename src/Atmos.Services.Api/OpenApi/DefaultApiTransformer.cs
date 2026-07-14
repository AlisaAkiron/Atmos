using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

public class DefaultApiTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var resp = new OpenApiResponses
        {
            ["200"] = new OpenApiResponse
            {
                Description = "System healthy",
                Content =  new Dictionary<string, IOpenApiMediaType>
                {
                    ["text/plain"] = new OpenApiMediaType()
                    {
                        Example = "Health"
                    }
                }
            },
            ["503"] = new OpenApiResponse
            {
                Description = "System unhealthy",
                Content = new Dictionary<string, IOpenApiMediaType>
                {
                    ["text/plain"] = new OpenApiMediaType()
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
                    Description = "Get to know if the system is running.",
                    Responses = resp
                }
            }
        });

        return Task.CompletedTask;
    }
}
