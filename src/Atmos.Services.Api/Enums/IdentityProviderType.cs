namespace Atmos.Services.Api.Enums;

/// <summary>
/// Which flow signs a caller in. OAuth and OpenIdConnect start at the login route; WebAuthn
/// runs its own assertion ceremony instead.
/// </summary>
public enum IdentityProviderType
{
    OAuth,
    OpenIdConnect,
    WebAuthn
}
