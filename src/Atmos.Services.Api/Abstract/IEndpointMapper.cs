using Microsoft.AspNetCore.Routing;

namespace Atmos.Services.Api.Abstract;

public interface IEndpointMapper
{
    public static abstract void MapEndpoints(IEndpointRouteBuilder endpoints);
}
