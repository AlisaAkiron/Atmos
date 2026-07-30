using System.Security.Claims;
using Atmos.Domain.Entities.Identity;

namespace Atmos.Services.Api.Abstract;

public interface IUserAccountService
{
    /// <summary>
    /// Build the local <see cref="ClaimsPrincipal"/> that gets signed into the cookie scheme.
    /// </summary>
    public ClaimsPrincipal CreatePrincipal(User user, string authenticationMethod);

    /// <summary>
    /// Find the local user linked to an external login, creating user and link on first sign-in.
    /// </summary>
    public Task<User> GetOrCreateFromExternalLoginAsync(string provider, ClaimsPrincipal externalPrincipal);
}
