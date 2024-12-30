using System.Text.Json.Serialization;
using Fido2NetLib;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;

public record WebAuthnAssertionDto
{
    [JsonPropertyName("options")]
    public AssertionOptions Options { get; set; } = null!;

    [JsonPropertyName("challenge_id")]
    public Guid ChallengeId { get; set; }
}
