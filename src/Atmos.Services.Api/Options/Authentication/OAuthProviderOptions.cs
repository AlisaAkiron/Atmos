using Atmos.Services.Api.Enums;

namespace Atmos.Services.Api.Options.Authentication;

public record OAuthProviderOptions
{
    public OAuthProviderType Type { get; set; }

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
