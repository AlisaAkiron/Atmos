using System.Text.Json.Serialization;

namespace Atmos.Api.Endpoints.Dto;

public record MagicLinkSendDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;
}
