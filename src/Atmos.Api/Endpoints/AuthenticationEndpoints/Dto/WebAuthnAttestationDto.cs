using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;

public record WebAuthnAttestationDto
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("is_creating_new_user")]
    public bool IsCreatingNewUser { get; set; }

    [JsonPropertyName("attestation_id")]
    public Guid AttestationId { get; set; }

    [JsonPropertyName("options")]
    public CredentialCreateOptions Options { get; set; } = null!;
}
