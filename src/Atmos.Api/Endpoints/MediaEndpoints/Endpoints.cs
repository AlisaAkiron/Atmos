using Atmos.Services.Api.Abstract;

namespace Atmos.Api.Endpoints;

public partial class MediaEndpoints : IEndpointMapper
{
    /// <summary>
    /// Every media endpoint is admin-only; the routes and handlers live in
    /// Endpoints.Admin.cs.
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapAdminEndpoints(endpoints);
    }
}
