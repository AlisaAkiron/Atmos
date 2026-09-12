using System.Text.Json.Serialization;

namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The signed-in account, as the caller needs to render its own session.
/// </summary>
public record AccountDto
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("email_addresses")]
    public List<string> EmailAddresses { get; set; } = [];

    [JsonPropertyName("is_site_owner")]
    public bool IsSiteOwner { get; set; }

    /// <summary>
    /// How this session authenticated: a provider name, or "webauthn".
    /// </summary>
    [JsonPropertyName("authentication_method")]
    public string? AuthenticationMethod { get; set; }
}

/// <summary>
/// Every credential that can reach the account, so the owner can audit and revoke.
/// </summary>
public record AccountCredentialsDto
{
    [JsonPropertyName("social_logins")]
    public List<SocialLoginDto> SocialLogins { get; set; } = [];

    [JsonPropertyName("webauthn_credentials")]
    public List<WebAuthnCredentialDto> WebAuthnCredentials { get; set; } = [];
}

/// <summary>
/// One linked external login.
/// </summary>
public record SocialLoginDto
{
    [JsonPropertyName("connection_id")]
    public Guid ConnectionId { get; set; }

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// One registered WebAuthn credential.
/// </summary>
public record WebAuthnCredentialDto
{
    /// <summary>
    /// Base64url encoded credential ID, which is also how it is addressed for removal.
    /// </summary>
    [JsonPropertyName("credential_id")]
    public string CredentialId { get; set; } = string.Empty;

    [JsonPropertyName("cred_type")]
    public string CredType { get; set; } = string.Empty;

    [JsonPropertyName("aa_guid")]
    public Guid AaGuid { get; set; }

    [JsonPropertyName("create_at")]
    public DateTimeOffset CreateAt { get; set; }
}
