using System.Text.Json.Serialization;
using Atmos.Services.Api.Enums;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;

public class AuthenticationProviderDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = null!;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IdentityProviderType Type { get; set; }
}
