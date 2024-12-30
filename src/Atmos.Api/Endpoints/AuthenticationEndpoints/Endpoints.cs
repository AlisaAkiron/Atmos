using Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;
using Atmos.Common.Extensions;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using AuthenticationOptions = Atmos.Services.Api.Options.Authentication.AuthenticationOptions;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints;

public partial class Endpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var authGroup = endpoints.MapGroup("/auth")
            .HasApiVersion(1)
            .WithTags("Authentication");

        authGroup.MapGet("/providers", GetProviders);
        authGroup.MapGet("/login/{provider}", InitiateAuthentication);

        // WebAuthn
        var webAuthnGroup = authGroup.MapGroup("/webauthn");

        webAuthnGroup.MapPost("/attestation", AttestationAsync);
        webAuthnGroup.MapPost("/attestation/{attestationId:guid}", AttestationVerifyAsync);
        webAuthnGroup.MapPost("/assertion", AssertionAsync);
        webAuthnGroup.MapPost("/assertion/{challengeId:guid}", AssertionVerifyAsync);
    }

    [EndpointSummary("Get authentication providers")]
    private static Ok<List<AuthenticationProviderDto>> GetProviders(
        [FromServices] IConfiguration configuration)
    {
        var result = GetAuthenticationProviderList(configuration);

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Initiate authentication")]
    private static Results<ChallengeHttpResult, NotFound> InitiateAuthentication(
        [FromServices] IConfiguration configuration,
        [FromRoute(Name = "provider")] string provider,
        [FromQuery(Name = "return_url")] string returnUrl)
    {
        var providers = GetAuthenticationProviderList(configuration);
        if (providers.Any(x => x.Name == provider) is false)
        {
            return TypedResults.NotFound();
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl,
            Items =
            {
                ["LoginProvider"] = provider,
                ["returnUrl"] = returnUrl
            }
        };

        return TypedResults.Challenge(properties, [provider]);
    }

    private static List<AuthenticationProviderDto> GetAuthenticationProviderList(IConfiguration configuration)
    {
        var options = configuration.GetOptions<AuthenticationOptions>("Authentication");
        var result = new List<AuthenticationProviderDto>();

        // OpenID Connect
        result.AddRange(options.OpenIdConnect
            .Select(oidc => new AuthenticationProviderDto
            {
                Name = oidc.Name,
                DisplayName = oidc.DisplayName,
                Type = IdentityProviderType.OAuth
            }));

        // OAuth
        result.AddRange(options.OAuthProviders
            .Select(oAuth => new AuthenticationProviderDto
            {
                Name = oAuth.Name,
                DisplayName = oAuth.DisplayName,
                Type = IdentityProviderType.OAuth
            }));

        // WebAuthn
        if (options.WebAuthn.Enable)
        {
            result.Add(new AuthenticationProviderDto
            {
                Name = "WebAuthn",
                DisplayName = "WebAuthn",
                Type = IdentityProviderType.WebAuthn
            });
        }

        return result;
    }
}
