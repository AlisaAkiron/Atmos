namespace Atmos.Services.Api.Options.Authentication;

public record OpenIdConnectOptions
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public string? MetadataAddress { get; set; }

    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? UserInfoEndpoint { get; set; }

    public List<string> Scopes { get; set; } = ["openid", "profile", "email"];
}
