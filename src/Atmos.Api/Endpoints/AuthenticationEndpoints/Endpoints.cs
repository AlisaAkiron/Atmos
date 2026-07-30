using Atmos.Api.Endpoints.Dto;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AuthenticationOptions = Atmos.Services.Api.Options.Authentication.AuthenticationOptions;

namespace Atmos.Api.Endpoints;

public partial class AuthenticationEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var authGroup = endpoints.MapGroup("/auth")
            .HasApiVersion(1)
            .WithTags("Authentication");

        authGroup.MapGet("/providers", GetProviders);
        authGroup.MapGet("/login/{provider}", InitiateAuthentication);
        authGroup.MapPost("/logout", Logout);

        // WebAuthn handlers resolve IFido2, which ConfigureIdentity only registers when
        // WebAuthn is enabled. Mapping them unconditionally turns that missing registration
        // into a 500 at request time, so leave the routes unmapped instead.
        var authenticationOptions = endpoints.ServiceProvider
            .GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        if (authenticationOptions.WebAuthn.Enable)
        {
            var webAuthnGroup = authGroup.MapGroup("/webauthn")
                .RequireRateLimiting(AtmosAuthenticationDefaults.RateLimitPolicy);

            webAuthnGroup.MapPost("/attestation", AttestationAsync);
            webAuthnGroup.MapPost("/attestation/{attestationId:guid}", AttestationVerifyAsync);
            webAuthnGroup.MapPost("/assertion", AssertionAsync);
            webAuthnGroup.MapPost("/assertion/{challengeId:guid}", AssertionVerifyAsync);
        }
    }

    [EndpointSummary("Get authentication providers")]
    private static Ok<List<AuthenticationProviderDto>> GetProviders(
        [FromServices] IOptions<AuthenticationOptions> authenticationOptions)
    {
        var result = GetAuthenticationProviderList(authenticationOptions.Value);

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Initiate authentication")]
    private static Results<ChallengeHttpResult, NotFound, BadRequest<string>> InitiateAuthentication(
        [FromServices] IOptions<AuthenticationOptions> authenticationOptions,
        [FromRoute(Name = "provider")] string provider,
        [FromQuery(Name = "return_url")] string? returnUrl)
    {
        returnUrl ??= "/";
        if (IsLocalUrl(returnUrl) is false)
        {
            return TypedResults.BadRequest("return_url must be a local URL");
        }

        // Only remote (challengeable) providers can be initiated here
        var providers = GetAuthenticationProviderList(authenticationOptions.Value);
        var known = providers.Any(x =>
            x.Name == provider &&
            x.Type is IdentityProviderType.OAuth or IdentityProviderType.OpenIdConnect);

        if (known is false)
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

    [EndpointSummary("Sign out")]
    private static SignOutHttpResult Logout()
    {
        return TypedResults.SignOut(new AuthenticationProperties(), [AtmosAuthenticationDefaults.Scheme]);
    }

    private static List<AuthenticationProviderDto> GetAuthenticationProviderList(AuthenticationOptions options)
    {
        var result = new List<AuthenticationProviderDto>();

        // OpenID Connect
        result.AddRange(options.OpenIdConnect
            .Select(oidc => new AuthenticationProviderDto
            {
                Name = oidc.Name,
                DisplayName = oidc.DisplayName,
                Type = IdentityProviderType.OpenIdConnect
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

    private static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/')
        {
            return false;
        }

        // "//host" and "/\host" are treated as protocol-relative absolute URLs by browsers
        return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
    }
}
