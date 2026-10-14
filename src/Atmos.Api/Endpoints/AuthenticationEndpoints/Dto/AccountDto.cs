using System.Text.Json.Serialization;

namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The signed-in account, as the caller needs to render its own session.
/// </summary>
public record AccountDto
{
    /// <summary>
    /// Stable identifier of the account.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Display name, taken from the identity provider that first created the account.
    /// </summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// Every email address known for the account, across all linked logins.
    /// </summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Whether this account holds the site owner role, which is what gates every admin route.
    /// </summary>
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
    /// <summary>
    /// Linked external logins, ordered by provider. The last one cannot be unlinked.
    /// </summary>
    [JsonPropertyName("social_logins")]
    public List<SocialLoginDto> SocialLogins { get; set; } = [];

    /// <summary>
    /// Registered WebAuthn credentials, oldest first.
    /// </summary>
    [JsonPropertyName("webauthn_credentials")]
    public List<WebAuthnCredentialDto> WebAuthnCredentials { get; set; } = [];
}

/// <summary>
/// One linked external login.
/// </summary>
public record SocialLoginDto
{
    /// <summary>
    /// Identifier of this link, which is how it is addressed for removal.
    /// </summary>
    [JsonPropertyName("connection_id")]
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Configured name of the identity provider behind this login.
    /// </summary>
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

    /// <summary>
    /// Credential type reported by the authenticator, in practice "public-key".
    /// </summary>
    [JsonPropertyName("cred_type")]
    public string CredType { get; set; } = string.Empty;

    /// <summary>
    /// Authenticator model identifier, which is what distinguishes one security key from another.
    /// </summary>
    [JsonPropertyName("aa_guid")]
    public Guid AaGuid { get; set; }

    /// <summary>
    /// When the credential was registered.
    /// </summary>
    [JsonPropertyName("create_at")]
    public DateTimeOffset CreateAt { get; set; }
}
