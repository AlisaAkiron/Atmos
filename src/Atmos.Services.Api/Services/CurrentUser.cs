using System.Security.Claims;
using Atmos.Domain.Abstract;
using Atmos.Domain.Entities.Identity;
using Atmos.Services.Api.Abstract;
using Microsoft.AspNetCore.Http;

namespace Atmos.Services.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserManager _userManager;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, IUserManager userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User ?? null;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public async Task<User?> GetUserAsync(bool includeDetails = false)
    {
        if (UserId.HasValue is false)
        {
            return null;
        }

        return await _userManager.GetUserAsync(UserId.Value, includeDetails);
    }
}
