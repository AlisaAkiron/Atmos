using System.ComponentModel;
using Atmos.Database;
using Atmos.Domain.Entities.Content;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Common.Dto;
using Atmos.Services.Api.Common.Mapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints.ArticleEndpoints;

public class ArticleEndpoints : IEndpointMapper
{
    /// <inheritdoc />
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var articleGroup = endpoints.MapGroup("/article")
            .HasApiVersion(1)
            .WithTags("Article");

        articleGroup.MapGet("/", GetArticlesAsync);
        articleGroup.MapGet("/{slug}", GetArticleAsync);
    }

    [EndpointSummary("Get all articles")]
    private static async Task<Ok<List<ArticleMetadataDto>>> GetArticlesAsync(
        [FromServices] AtmosDbContext dbContext,
        [FromQuery(Name = "classification"), Description("Article classification")]string? classification = null,
        [FromQuery(Name = "skip"), Description("Skip how many items")] int skip = 0,
        [FromQuery(Name = "take"), Description("Take how many items")] int take = 20)
    {
        IQueryable<Article> query = dbContext.Articles
            .Where(x => x.IsDeleted == false)
            .Where(x => x.IsDraft == false)
            .OrderBy(x => x.FirstReleaseTime);

        if (string.IsNullOrEmpty(classification) is false)
        {
            query = query
                .Include(x => x.Classification)
                .Where(x => x.Classification.Slug == classification);
        }

        query = query.Skip(skip).Take(take);

        var articles = await query.ToListAsync();
        var dtos = articles.Select(x => x.MapArticleMetadataDto()).ToList();

        return TypedResults.Ok(dtos);
    }

    [EndpointSummary("Get article by slug")]
    private static async Task<Results<Ok<ArticleDto>, NotFound>> GetArticleAsync(
        [FromServices] AtmosDbContext dbContext,
        [FromRoute(Name = "slug"), Description("Article slug")] string slug)
    {
        var article = await dbContext.Articles
            .Include(x => x.Classification)
            .Where(x => x.IsDeleted == false)
            .Where(x => x.IsDraft == false)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (article is null)
        {
            return TypedResults.NotFound();
        }

        var dto = article.MapArticleDto();
        return TypedResults.Ok(dto);
    }
}
