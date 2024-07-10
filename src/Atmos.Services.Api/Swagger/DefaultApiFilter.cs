using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Atmos.Services.Api.Swagger;

public class DefaultApiFilter : IDocumentFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var resp = new OpenApiResponses
        {
            ["200"] = new OpenApiResponse
            {
                Description = "System healthy",
                Content =
                {
                    ["text/plain"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString("Health")
                    }
                }
            },
            ["503"] = new OpenApiResponse
            {
                Description = "System unhealthy",
                Content =
                {
                    ["text/plain"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString("Unhealthy")
                    }
                }
            }
        };

        var tag = new OpenApiTag
        {
            Name = "Status"
        };

        swaggerDoc.Paths.Add("/health", new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = [tag],
                    Description = "Get to know if the system can serve requests.",
                    Responses = resp
                }
            }
        });

        swaggerDoc.Paths.Add("/alive", new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = [tag],
                    Description = "Get to know if the system is running.",
                    Responses = resp
                }
            }
        });
    }
}
