using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The authenticator's answer to a sign-in challenge.
/// </summary>
public record WebAuthnAssertionVerifyDto
{
    /// <summary>
    /// Raw result of navigator.credentials.get, passed through unchanged.
    /// </summary>
    [JsonPropertyName("assertion_response")]
    public AuthenticatorAssertionRawResponse AssertionResponse { get; set; } = null!;
}
