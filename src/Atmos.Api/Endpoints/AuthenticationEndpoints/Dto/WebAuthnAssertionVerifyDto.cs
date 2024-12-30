using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;

public record WebAuthnAssertionVerifyDto
{
    [JsonPropertyName("assertion_response")]
    public AuthenticatorAssertionRawResponse AssertionResponse { get; set; } = null!;
}
