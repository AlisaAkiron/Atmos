using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

public static class OpenApiConfigurator
{
    public static void ConfigureDocument(OpenApiOptions options)
    {
        options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;

        // Asp.Versioning's API explorer groups every endpoint mapped through
        // NewVersionedApi() under its API version. The default filter keeps only
        // ungrouped endpoints and ones grouped under the document's own name, and
        // this document is named after the service, so it would drop them all.
        options.ShouldInclude = _ => true;

        options.AddDocumentTransformer<DefaultApiTransformer>();
        options.AddOperationTransformer<ApiVersionHeaderTransformer>();
    }
}
