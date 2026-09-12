using System.Text.Json.Serialization;
using Atmos.Services.Api.Enums;

namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// One sign-in method this deployment has configured.
/// </summary>
public class AuthenticationProviderDto
{
    /// <summary>
    /// Configured name. For OAuth and OpenID Connect this is the provider segment of the login route.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Human readable name, for a sign-in button.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Which kind of method this is, and so which flow starts it. Only OAuth and OpenIdConnect
    /// can be initiated through the login route; WebAuthn runs its own assertion ceremony.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IdentityProviderType Type { get; set; }
}
