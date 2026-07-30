using Atmos.Api.Endpoints.Dto;
using Atmos.Database;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints;

public partial class ArticleEndpoints : IEndpointMapper
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/articles")
            .HasApiVersion(1)
            .WithTags("Articles");

        publicGroup.MapGet("/", GetArticles).CacheOutput(ContentCaching.Policies.Articles);
        publicGroup.MapGet("/{slug}", GetArticle).CacheOutput(ContentCaching.Policies.Articles);

        MapAdminEndpoints(endpoints);
    }

    [EndpointSummary("List published articles")]
    private static async Task<Ok<ArticleListResponse>> GetArticles(
        [FromServices] AtmosDbContext dbContext,
        [FromQuery(Name = "category")] string? category,
        [FromQuery(Name = "tag")] string? tag,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Articles.Where(x => x.PublishedAt != null);

        if (string.IsNullOrEmpty(category) is false)
        {
            query = query.Where(x => x.Category != null && x.Category.Slug == category);
        }

        if (string.IsNullOrEmpty(tag) is false)
        {
            query = query.Where(x => x.Tags.Any(t => t.Slug == tag));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ArticleListItemDto(
                x.Slug, x.Title, x.Summary,
                x.Category != null ? x.Category.Slug : null,
                x.Tags.Select(t => t.Slug).OrderBy(s => s).ToList(),
                x.PublishedAt!.Value))
            .ToListAsync(ct);

        return TypedResults.Ok(new ArticleListResponse(totalCount, page, pageSize, items));
    }

    [EndpointSummary("Get published article by slug")]
    private static async Task<Results<Ok<ArticleDto>, NotFound>> GetArticle(
        [FromServices] AtmosDbContext dbContext,
        [FromRoute] string slug,
        CancellationToken ct)
    {
        var article = await dbContext.Articles
            .Where(x => x.Slug == slug && x.PublishedAt != null)
            .Select(x => new ArticleDto(
                x.Slug, x.Title, x.Summary, x.Content,
                x.Category != null ? x.Category.Slug : null,
                x.Tags.Select(t => t.Slug).OrderBy(s => s).ToList(),
                x.PublishedAt!.Value))
            .FirstOrDefaultAsync(ct);

        return article is null ? TypedResults.NotFound() : TypedResults.Ok(article);
    }
}
