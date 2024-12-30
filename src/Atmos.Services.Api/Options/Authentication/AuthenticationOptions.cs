namespace Atmos.Services.Api.Options.Authentication;

public record AuthenticationOptions
{
    public List<OpenIdConnectOptions> OpenIdConnect { get; set; } = [];

    public List<OAuthProviderOptions> OAuthProviders { get; set; } = [];

    public WebAuthnOptions WebAuthn { get; set; } = new();
}
