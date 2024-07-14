using Atmos.Database;
using Atmos.Domain.Entities.Content;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Common.Dto;
using Atmos.Services.Api.Common.Mapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Content.Endpoints.ArticleEndpoints;

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

    /// <summary>
    ///     Get all articles
    /// </summary>
    /// <param name="dbContext">DbContext from DI</param>
    /// <param name="classification">Article classification slug, null for no filter</param>
    /// <param name="skip">How many articles to skip</param>
    /// <param name="take">How many article to get, default to 20</param>
    /// <returns>A list of articles</returns>
    private static async Task<Ok<List<ArticleMetadataDto>>> GetArticlesAsync(
        [FromServices] AtmosDbContext dbContext,
        [FromQuery(Name = "classification")] string? classification = null,
        [FromQuery(Name = "skip")] int skip = 0,
        [FromQuery(Name = "take")] int take = 20)
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

    private static async Task<Results<Ok<ArticleDto>, NotFound>> GetArticleAsync(
        [FromServices] AtmosDbContext dbContext,
        [FromRoute(Name = "slug")] string slug)
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
