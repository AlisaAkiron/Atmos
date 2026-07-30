using System.Security.Claims;
using Atmos.Common.Extensions;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Enums;
using Atmos.Services.Api.Options.Authentication;
using Fido2NetLib;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AuthenticationOptions = Atmos.Services.Api.Options.Authentication.AuthenticationOptions;

namespace Atmos.Services.Api.Components;

public static class AtmosIdentity
{
    internal static IHostApplicationBuilder ConfigureIdentity(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AtmosAuthenticationDefaults.SiteOwnerPolicy,
                policy => policy.RequireRole(AtmosAuthenticationDefaults.SiteOwnerRole));
        });

        builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"));

        var authenticationBuilder = builder.Services.AddAuthentication(AtmosAuthenticationDefaults.Scheme);

        authenticationBuilder.AddCookie(AtmosAuthenticationDefaults.Scheme, o =>
        {
            o.Cookie.Name = AtmosAuthenticationDefaults.Scheme;
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromDays(14);
            o.SlidingExpiration = true;

            // This cookie authenticates an API: return status codes instead of
            // redirecting to a login page that does not exist
            o.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            o.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        var authenticationOptions = builder.Configuration
            .GetOptions<AuthenticationOptions>("Authentication");

        // OpenID Connect
        foreach (var oidc in authenticationOptions.OpenIdConnect)
        {
            authenticationBuilder.AddOpenIdConnect(oidc.Name, oidc.DisplayName, o =>
            {
                o.Scope.Clear();
                foreach (var scope in oidc.Scopes)
                {
                    o.Scope.Add(scope);
                }

                o.Authority = oidc.Authority;
                o.ClientId = oidc.ClientId;
                o.ClientSecret = oidc.ClientSecret;

                o.SaveTokens = true;
                o.GetClaimsFromUserInfoEndpoint = true;
                o.TokenValidationParameters.LogValidationExceptions = true;

                o.MetadataAddress = oidc.MetadataAddress;

                o.CallbackPath = $"/auth/callback/{oidc.Name}";

                var claimMappings = oidc.ClaimMappings;
                o.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context => OnTicketReceivedAsync(context, oidc.Name, claimMappings)
                };
            });
        }

        // OAuth
        foreach (var oauth in authenticationOptions.OAuthProviders)
        {
            switch (oauth.Type)
            {
                case OAuthProviderType.GitHub:
                    authenticationBuilder.AddGitHub(oauth.Name, oauth.DisplayName, o =>
                    {
                        o.ClientId = oauth.ClientId;
                        o.ClientSecret = oauth.ClientSecret;
                        o.CallbackPath = $"/auth/callback/{oauth.Name}";
                        o.Events.OnTicketReceived = context => OnTicketReceivedAsync(context, oauth.Name, null);
                    });
                    break;
                case OAuthProviderType.Discord:
                    authenticationBuilder.AddDiscord(oauth.Name, oauth.DisplayName, o =>
                    {
                        o.ClientId = oauth.ClientId;
                        o.ClientSecret = oauth.ClientSecret;
                        o.CallbackPath = $"/auth/callback/{oauth.Name}";
                        o.Events.OnTicketReceived = context => OnTicketReceivedAsync(context, oauth.Name, null);
                    });
                    break;
                case OAuthProviderType.Microsoft:
                    authenticationBuilder.AddMicrosoftAccount(oauth.Name, oauth.DisplayName, o =>
                    {
                        o.ClientId = oauth.ClientId;
                        o.ClientSecret = oauth.ClientSecret;
                        o.CallbackPath = $"/auth/callback/{oauth.Name}";
                        o.Events.OnTicketReceived = context => OnTicketReceivedAsync(context, oauth.Name, null);
                    });
                    break;
                case OAuthProviderType.Google:
                    authenticationBuilder.AddGoogle(oauth.Name, oauth.DisplayName, o =>
                    {
                        o.ClientId = oauth.ClientId;
                        o.ClientSecret = oauth.ClientSecret;
                        o.CallbackPath = $"/auth/callback/{oauth.Name}";
                        o.Events.OnTicketReceived = context => OnTicketReceivedAsync(context, oauth.Name, null);
                    });
                    break;
                default:
                    throw new InvalidOperationException($"Unknown OAuth provider type: {oauth.Type}");
            }
        }

        // WebAuthn
        var webAuthn = authenticationOptions.WebAuthn;
        if (webAuthn.Enable)
        {
            var fido2Configuration = new Fido2Configuration
            {
                ServerName = webAuthn.ServerName,
                ServerIcon = webAuthn.ServerIcon,
                ServerDomain = webAuthn.ServerDomain,
                Origins = webAuthn.Origins.ToHashSet()
            };

            builder.Services.AddSingleton<IFido2>(new Fido2(fido2Configuration));
        }

        return builder;
    }

    /// <summary>
    /// Turns the external provider's principal into a local one: remaps configured claims,
    /// provisions the local user and social login link on first sign-in, then replaces the
    /// principal so the cookie only ever carries local identity claims.
    /// </summary>
    private static async Task OnTicketReceivedAsync(TicketReceivedContext context, string provider, ClaimMappingOptions? claimMappings)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        if (claimMappings is not null)
        {
            RemapClaim(identity, claimMappings.Sub, ClaimTypes.NameIdentifier);
            RemapClaim(identity, claimMappings.Name, ClaimTypes.Name);
            RemapClaim(identity, claimMappings.Email, ClaimTypes.Email);
        }

        var accountService = context.HttpContext.RequestServices.GetRequiredService<IUserAccountService>();

        var user = await accountService.GetOrCreateFromExternalLoginAsync(provider, context.Principal);
        context.Principal = accountService.CreatePrincipal(user, provider);
    }

    private static void RemapClaim(ClaimsIdentity identity, string? sourceClaimType, string targetClaimType)
    {
        if (string.IsNullOrEmpty(sourceClaimType)) return;

        var sourceClaim = identity.FindFirst(sourceClaimType);
        if (sourceClaim is null) return;

        var existing = identity.FindFirst(targetClaimType);
        if (existing is not null)
            identity.RemoveClaim(existing);

        identity.AddClaim(new Claim(targetClaimType, sourceClaim.Value, sourceClaim.ValueType,
            sourceClaim.Issuer, sourceClaim.OriginalIssuer));
    }
}
