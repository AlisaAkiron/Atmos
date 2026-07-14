namespace Atmos.Services.Api.Options.Authentication;

public record OpenIdConnectOptions
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public string MetadataAddress { get; set; } = string.Empty;

    public List<string> Scopes { get; set; } = [];

    public ClaimMappingOptions ClaimMappings { get; set; } = new();
}

public record ClaimMappingOptions
{
    public string? Sub { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }
}
