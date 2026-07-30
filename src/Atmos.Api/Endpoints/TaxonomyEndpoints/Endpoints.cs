using Atmos.Api.Endpoints.Dto;
using Atmos.Database;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints;

public partial class TaxonomyEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/categories")
            .HasApiVersion(1)
            .WithTags("Taxonomy")
            .MapGet("/", GetCategories)
            .CacheOutput(ContentCaching.Policies.Taxonomy);

        endpoints.MapGroup("/tags")
            .HasApiVersion(1)
            .WithTags("Taxonomy")
            .MapGet("/", GetTags)
            .CacheOutput(ContentCaching.Policies.Taxonomy);

        MapAdminEndpoints(endpoints);
    }

    [EndpointSummary("List categories")]
    private static async Task<Ok<List<CategoryDto>>> GetCategories(
        [FromServices] AtmosDbContext dbContext,
        CancellationToken ct)
    {
        var result = await dbContext.Categories
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto(x.Slug, x.Name, x.Description,
                x.Articles.Count(a => a.PublishedAt != null)))
            .ToListAsync(ct);

        return TypedResults.Ok(result);
    }

    [EndpointSummary("List tags")]
    private static async Task<Ok<List<TagDto>>> GetTags(
        [FromServices] AtmosDbContext dbContext,
        CancellationToken ct)
    {
        var result = await dbContext.Tags
            .OrderBy(x => x.Name)
            .Select(x => new TagDto(x.Slug, x.Name,
                x.Articles.Count(a => a.PublishedAt != null)))
            .ToListAsync(ct);

        return TypedResults.Ok(result);
    }
}
