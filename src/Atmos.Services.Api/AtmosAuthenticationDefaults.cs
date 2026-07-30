namespace Atmos.Services.Api;

public static class AtmosAuthenticationDefaults
{
    /// <summary>
    /// The cookie authentication scheme every sign-in flow ultimately signs into.
    /// </summary>
    public const string Scheme = "atmos";

    /// <summary>
    /// Claim type recording which method (provider name, "webauthn") authenticated the session.
    /// </summary>
    public const string AuthenticationMethodClaimType = "atmos:authentication_method";

    /// <summary>
    /// Rate limiting policy applied to credential-related authentication endpoints.
    /// </summary>
    public const string RateLimitPolicy = "authentication";
}
