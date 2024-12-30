using Atmos.Services.Api.Enums;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;

public class AuthenticationProviderDto
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public IdentityProviderType Type { get; set; }
}
