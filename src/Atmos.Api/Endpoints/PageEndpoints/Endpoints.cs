using Atmos.Api.Endpoints.Dto;
using Atmos.Database;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints;

public partial class PageEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/pages")
            .HasApiVersion(1)
            .WithTags("Pages");

        publicGroup.MapGet("/{slug}", GetPage).CacheOutput(ContentCaching.Policies.Pages);

        MapAdminEndpoints(endpoints);
    }

    [EndpointSummary("Get page by slug")]
    private static async Task<Results<Ok<PageDto>, NotFound>> GetPage(
        [FromServices] AtmosDbContext dbContext,
        [FromRoute] string slug,
        CancellationToken ct)
    {
        var page = await dbContext.Pages
            .Where(x => x.Slug == slug && x.PublishedAt != null)
            .Select(x => new PageDto(x.Slug, x.Title, x.Content, x.PublishedAt!.Value))
            .FirstOrDefaultAsync(ct);

        return page is null ? TypedResults.NotFound() : TypedResults.Ok(page);
    }
}
