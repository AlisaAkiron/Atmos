using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

/// <summary>
/// Describes IFormFile. It is a framework type, so it carries no XML comments of its own and
/// would otherwise be the one schema in the document with nothing said about it.
/// </summary>
public class FormFileTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(IFormFile))
        {
            schema.Description = "The file itself, sent as a multipart/form-data part.";
        }

        return Task.CompletedTask;
    }
}
