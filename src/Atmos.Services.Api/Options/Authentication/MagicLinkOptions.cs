namespace Atmos.Services.Api.Options.Authentication;

public record MagicLinkOptions
{
    public bool Enable { get; set; }

    public int TokenLength { get; set; } = 8;

    public int ExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// Absolute base URL the emailed link points at (e.g. a frontend page that submits the token).
    /// Required outside Development: emailed links are never derived from the request's Host
    /// header there, since a forged Host would redirect live tokens to an attacker's domain.
    /// In Development only, an empty value falls back to the API's own GET verify endpoint.
    /// </summary>
    public string? LinkBaseUrl { get; set; }

    /// <summary>
    /// Local URL to redirect to after a successful GET-link sign-in.
    /// </summary>
    public string SignedInRedirectUrl { get; set; } = "/";
}
