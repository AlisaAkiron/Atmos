namespace Atmos.Services.Api.Options.Authentication;

public record AuthenticationOptions
{
    public List<OpenIdConnectOptions> OpenIdConnect { get; set; } = [];

    public List<OAuthProviderOptions> OAuthProviders { get; set; } = [];

    public WebAuthnOptions WebAuthn { get; set; } = new();

    /// <summary>
    /// Origins a completed sign-in may return the browser to, e.g.
    /// <c>https://atmos.example.com</c>.
    /// </summary>
    /// <remarks>
    /// Only needed when the frontend is served from an origin of its own. Empty
    /// permits local paths only, which is what a frontend proxied onto the API's
    /// origin needs and what the deployment does by default.
    /// </remarks>
    public List<string> ReturnUrls { get; set; } = [];
}
