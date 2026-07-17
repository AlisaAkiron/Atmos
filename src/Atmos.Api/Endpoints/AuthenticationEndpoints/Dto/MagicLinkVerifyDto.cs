using System.Text.Json.Serialization;

namespace Atmos.Api.Endpoints.Dto;

public record MagicLinkVerifyDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}
