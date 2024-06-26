using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Atmos.Services.Api.Swagger;

public class ApiVersionHeaderFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Atmos-Api-Version",
            Required = false,
            In = ParameterLocation.Header
        });
    }
}
