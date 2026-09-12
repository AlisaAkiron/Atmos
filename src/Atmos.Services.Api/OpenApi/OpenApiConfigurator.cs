using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

public static class OpenApiConfigurator
{
    /// <summary>
    /// Name of the single OpenAPI document Atmos serves, and the Scalar route that renders it.
    /// </summary>
    public static string DocumentName(IConfiguration configuration)
    {
        return configuration["OTEL_SERVICE_NAME"] ?? "Unknown";
    }

    /// <summary>
    /// Configures the document. Call this from the AddOpenApi() in the project that owns the
    /// endpoints: Microsoft.AspNetCore.OpenApi's XML comment support is a source generator that
    /// intercepts that call site, and it can only see comments in its own compilation.
    /// </summary>
    public static void ConfigureDocument(OpenApiOptions options)
    {
        options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;

        // Asp.Versioning's API explorer groups every endpoint mapped through
        // NewVersionedApi() under its API version. The default filter keeps only
        // ungrouped endpoints and ones grouped under the document's own name, and
        // this document is named after the service, so it would drop them all.
        options.ShouldInclude = _ => true;

        options.AddDocumentTransformer<DefaultApiTransformer>();
        options.AddDocumentTransformer<AuthenticationTransformer>();
        options.AddOperationTransformer<ApiVersionHeaderTransformer>();
        options.AddOperationTransformer<AuthenticationTransformer>();
        options.AddSchemaTransformer<FormFileTransformer>();
    }
}
