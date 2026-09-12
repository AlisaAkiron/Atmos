using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The authenticator's answer to a registration challenge.
/// </summary>
public record WebAuthnAttestationVerifyDto
{
    /// <summary>
    /// Raw result of navigator.credentials.create, passed through unchanged.
    /// </summary>
    [JsonPropertyName("attestation_response")]
    public AuthenticatorAttestationRawResponse AttestationResponse { get; set; } = null!;
}
