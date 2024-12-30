using Atmos.Common.Extensions;
using Atmos.Services.Api.Enums;
using Atmos.Services.Api.Options.Authentication;
using Fido2NetLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Atmos.Services.Api.Components;

public static class AtmosIdentity
{
    internal static IHostApplicationBuilder ConfigureIdentity(this IHostApplicationBuilder builder)
    {
        const string defaultScheme = "atmos";

        builder.Services.AddAuthorization();
        var authenticationBuilder = builder.Services.AddAuthentication(defaultScheme);

        authenticationBuilder.AddCookie("atmos", o =>
        {
            o.Cookie.Name = "atmos";
            o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
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

                o.CallbackPath = $"/auth/callback/{oidc.Name}";

                if (string.IsNullOrEmpty(oidc.MetadataAddress) is false)
                {
                    o.MetadataAddress = oidc.MetadataAddress;
                }
                else
                {
                    if (string.IsNullOrEmpty(oidc.AuthorizationEndpoint) ||
                        string.IsNullOrEmpty(oidc.TokenEndpoint) ||
                        string.IsNullOrEmpty(oidc.UserInfoEndpoint))
                    {
                        throw new InvalidOperationException("MetadataAddress or required endpoints are not provided.");
                    }

                    o.Configuration = new OpenIdConnectConfiguration
                    {
                        AuthorizationEndpoint = oidc.AuthorizationEndpoint,
                        TokenEndpoint = oidc.TokenEndpoint,
                        UserInfoEndpoint = oidc.UserInfoEndpoint
                    };
                }
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
                    });
                    break;
                case OAuthProviderType.Discord:
                    authenticationBuilder.AddDiscord(oauth.Name, oauth.DisplayName, o =>
                    {
                        o.ClientId = oauth.ClientId;
                        o.ClientSecret = oauth.ClientSecret;
                        o.CallbackPath = $"/auth/callback/{oauth.Name}";
                    });
                    break;
                case OAuthProviderType.Microsoft:
                    authenticationBuilder.AddMicrosoftAccount(oauth.Name, oauth.DisplayName, o =>
                    {
                        o.ClientId = oauth.ClientId;
                        o.ClientSecret = oauth.ClientSecret;
                        o.CallbackPath = $"/auth/callback/{oauth.Name}";
                    });
                    break;
                case OAuthProviderType.Google:
                    authenticationBuilder.AddGoogle(oauth.Name, oauth.DisplayName, o =>
                    {
                        o.ClientId = oauth.ClientId;
                        o.ClientSecret = oauth.ClientSecret;
                        o.CallbackPath = $"/auth/callback/{oauth.Name}";
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
            var fido2 = new Fido2(fido2Configuration);

            builder.Services.AddSingleton<IFido2, Fido2>(_ => fido2);
        }

        return builder;
    }
}
