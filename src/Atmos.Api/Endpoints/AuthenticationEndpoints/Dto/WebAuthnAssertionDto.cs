using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// Start of a WebAuthn sign-in: the challenge to sign, and the ID to send it back under.
/// </summary>
public record WebAuthnAssertionDto
{
    /// <summary>
    /// Options to pass to navigator.credentials.get. No credential hint is included, so the
    /// authenticator offers its own discoverable credentials.
    /// </summary>
    [JsonPropertyName("options")]
    public AssertionOptions Options { get; set; } = null!;

    /// <summary>
    /// Identifies this ceremony on the verify route. Single-use, and expires after five minutes.
    /// </summary>
    [JsonPropertyName("challenge_id")]
    public Guid ChallengeId { get; set; }
}
