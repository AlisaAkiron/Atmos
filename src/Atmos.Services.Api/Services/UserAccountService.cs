using System.Security.Claims;
using Atmos.Common.Abstract;
using Atmos.Common.Utils;
using Atmos.Domain.Abstract;
using Atmos.Domain.Entities.Identity;
using Atmos.Services.Api.Abstract;

namespace Atmos.Services.Api.Services;

public class UserAccountService : IUserAccountService
{
    private readonly IUserManager _userManager;
    private readonly IGuidProvider _guidProvider;

    public UserAccountService(IUserManager userManager, IGuidProvider guidProvider)
    {
        _userManager = userManager;
        _guidProvider = guidProvider;
    }

    public ClaimsPrincipal CreatePrincipal(User user, string authenticationMethod)
    {
        var identity = new ClaimsIdentity(AtmosAuthenticationDefaults.Scheme, ClaimTypes.Name, ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Nickname));
        identity.AddClaim(new Claim(AtmosAuthenticationDefaults.AuthenticationMethodClaimType, authenticationMethod));
        identity.AddClaim(new Claim(AtmosAuthenticationDefaults.SecurityStampClaimType, user.SecurityStamp.ToString()));

        var email = user.EmailAddress;
        if (email is not null)
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        if (user.IsSiteOwner)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, AtmosAuthenticationDefaults.SiteOwnerRole));
        }

        return new ClaimsPrincipal(identity);
    }

    public async Task<User> GetOrCreateFromExternalLoginAsync(string provider, ClaimsPrincipal externalPrincipal)
    {
        var identifier = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? throw new InvalidOperationException(
                             $"External principal from '{provider}' does not contain a name identifier claim");

        var existing = await _userManager.GetUserBySocialLoginAsync(provider, identifier, true);
        if (existing is not null)
        {
            return existing;
        }

        var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);
        email = string.IsNullOrWhiteSpace(email) || EmailUtils.IsValid(email) is false
            ? null
            : EmailUtils.Normalize(email);

        var nickname = externalPrincipal.FindFirstValue(ClaimTypes.Name)
                       ?? GetNicknameFromEmail(email)
                       ?? RandomUtils.GetRandomAlphabetString(6);

        // Never auto-link to an existing account by email: providers do not always verify
        // email ownership, and silently attaching a login would enable account takeover.
        // The address is dropped rather than the sign-in refused - the new account is
        // legitimate, it just does not get to claim an address someone else holds.
        if (email is not null && await _userManager.GetUserByEmailAsync(email) is not null)
        {
            email = null;
        }

        var user = await _userManager.CreateUserAsync(_guidProvider.Create(), nickname, email, true);
        await _userManager.AddSocialLoginAsync(user, provider, identifier);

        return user;
    }

    private static string? GetNicknameFromEmail(string? email)
    {
        if (email is null)
        {
            return null;
        }

        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        return atIndex > 0 ? email[..atIndex] : null;
    }
}
