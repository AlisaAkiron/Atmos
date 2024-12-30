using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;

public record WebAuthnAttestationVerifyDto
{
    [JsonPropertyName("attestation_response")]
    public AuthenticatorAttestationRawResponse AttestationResponse { get; set; } = null!;
}
