using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.Dto;

public record WebAuthnAttestationDto
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("attestation_id")]
    public Guid AttestationId { get; set; }

    [JsonPropertyName("options")]
    public CredentialCreateOptions Options { get; set; } = null!;
}
