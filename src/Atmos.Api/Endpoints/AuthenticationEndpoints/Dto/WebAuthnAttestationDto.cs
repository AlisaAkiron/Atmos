using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// Start of a WebAuthn registration: the credential to create, and the ID to send it back under.
/// </summary>
public record WebAuthnAttestationDto
{
    /// <summary>
    /// Account the credential will be registered against, always the signed-in one.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Name the authenticator shows while prompting.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Identifies this ceremony on the verify route. Single-use, and expires after five minutes.
    /// </summary>
    [JsonPropertyName("attestation_id")]
    public Guid AttestationId { get; set; }

    /// <summary>
    /// Options to pass to navigator.credentials.create. A resident, user-verifying credential is
    /// required, because that is what sign-in later asks for.
    /// </summary>
    [JsonPropertyName("options")]
    public CredentialCreateOptions Options { get; set; } = null!;
}
