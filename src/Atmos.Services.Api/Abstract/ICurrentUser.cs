using System.Security.Claims;
using Atmos.Domain.Entities.Identity;

namespace Atmos.Services.Api.Abstract;

public interface ICurrentUser
{
    public ClaimsPrincipal? Principal { get; }
    public bool IsAuthenticated { get; }
    public Guid? UserId { get; }

    public Task<User?> GetUserAsync(bool includeDetails = false);
}
