using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Atmos.Services.Api.OpenApi;

/// <summary>
/// Declares the session cookie as a security scheme and marks the operations that need it.
/// Authorization lives on the endpoint groups, so the requirement is read back from endpoint
/// metadata rather than repeated by hand on every operation.
/// </summary>
public class AuthenticationTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string SchemeName = AtmosAuthenticationDefaults.Scheme;

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = AtmosAuthenticationDefaults.Scheme,
            Description = "Session cookie issued by the sign-in flow at /api/auth/login/{provider}. " +
                          "Browsers send it automatically; it cannot be set from a script because it is HttpOnly."
        };

        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var authorizeData = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .ToList();

        if (authorizeData.Count == 0)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            // Without the host document the reference cannot resolve, and the requirement
            // serialises as an empty object that no reader can tie back to the scheme
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = []
        });

        operation.Responses ??= [];
        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "No session cookie was sent, or the session has expired."
        });

        // A policy means the session is not enough on its own: it also has to carry the role
        var requiresPolicy = authorizeData.Exists(data => string.IsNullOrEmpty(data.Policy) is false);

        if (requiresPolicy)
        {
            operation.Responses.TryAdd("403", new OpenApiResponse
            {
                Description = "The session is valid but the account is not the site owner."
            });
        }

        return Task.CompletedTask;
    }
}
