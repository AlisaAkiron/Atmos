using Atmos.Api.Endpoints.Dto;
using Atmos.Common.Abstract;
using Atmos.Common.Utils;
using Atmos.Database;
using Atmos.Domain.Entities.Content;
using Atmos.Domain.Enums;
using Atmos.Services.Api;
using Atmos.Services.Api.Models;
using Atmos.Services.Media.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints;

public partial class ArticleEndpoints
{
    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/admin/articles")
            .HasApiVersion(1)
            .WithTags("Articles Admin")
            .RequireAuthorization(AtmosAuthenticationDefaults.SiteOwnerPolicy);

        adminGroup.MapGet("/", GetArticlesAdmin);
        adminGroup.MapGet("/{id:guid}", GetArticleAdmin);
        adminGroup.MapPost("/", CreateArticle);
        adminGroup.MapPut("/{id:guid}", UpdateArticle);
        adminGroup.MapDelete("/{id:guid}", DeleteArticle);
        adminGroup.MapPost("/{id:guid}/publish", PublishArticle);
        adminGroup.MapPost("/{id:guid}/unpublish", UnpublishArticle);
    }

    [EndpointSummary("List articles (admin)")]
    private static async Task<Ok<ArticleAdminListResponse>> GetArticlesAdmin(
        [FromServices] AtmosDbContext dbContext,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await dbContext.Articles.CountAsync(ct);

        var items = await dbContext.Articles
            .OrderByDescending(x => x.UpdateAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ArticleAdminListItemDto(
                x.ArticleId, x.Slug, x.Title,
                x.Category != null ? x.Category.Slug : null,
                x.Tags.Select(t => t.Slug).OrderBy(s => s).ToList(),
                x.PublishedAt, x.UpdateAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new ArticleAdminListResponse(totalCount, page, pageSize, items));
    }

    [EndpointSummary("Get article (admin)")]
    private static async Task<Results<Ok<ArticleAdminDto>, NotFound>> GetArticleAdmin(
        [FromServices] AtmosDbContext dbContext,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var article = await dbContext.Articles
            .Include(x => x.Category)
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.ArticleId == id, ct);

        return article is null ? TypedResults.NotFound() : TypedResults.Ok(ToAdminDto(article));
    }

    [EndpointSummary("Create article")]
    private static async Task<Results<Created<ArticleAdminDto>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateArticle(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IGuidProvider guidProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromBody] ArticleRequest request,
        CancellationToken ct)
    {
        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var exists = await dbContext.Articles.AnyAsync(x => x.Slug == request.Slug, ct);
        if (exists)
        {
            return TypedResults.Conflict(new ErrorResponse($"Article slug '{request.Slug}' already exists"));
        }

        var resolved = await ResolveTaxonomyAsync(dbContext, request, ct);
        if (resolved.Error is not null)
        {
            return TypedResults.BadRequest(resolved.Error);
        }

        var article = new Article
        {
            ArticleId = guidProvider.Create(),
            Slug = request.Slug,
            Title = request.Title,
            Summary = request.Summary,
            Content = request.Content,
            CategoryId = resolved.Category?.CategoryId,
            Category = resolved.Category,
            Tags = resolved.Tags
        };

        await dbContext.Articles.AddAsync(article, ct);
        await referenceService.SetMarkdownReferencesAsync(MediaReferrerType.Article, article.ArticleId, article.Content, ct);
        await dbContext.SaveChangesAsync(ct);
        await EvictAsync(cacheStore, ct);

        return TypedResults.Created($"/api/admin/articles/{article.ArticleId}", ToAdminDto(article));
    }

    [EndpointSummary("Update article")]
    private static async Task<Results<Ok<ArticleAdminDto>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> UpdateArticle(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        [FromBody] ArticleRequest request,
        CancellationToken ct)
    {
        var article = await dbContext.Articles
            .Include(x => x.Category)
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.ArticleId == id, ct);

        if (article is null)
        {
            return TypedResults.NotFound();
        }

        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var slugTaken = await dbContext.Articles.AnyAsync(x => x.Slug == request.Slug && x.ArticleId != id, ct);
        if (slugTaken)
        {
            return TypedResults.Conflict(new ErrorResponse($"Article slug '{request.Slug}' already exists"));
        }

        var resolved = await ResolveTaxonomyAsync(dbContext, request, ct);
        if (resolved.Error is not null)
        {
            return TypedResults.BadRequest(resolved.Error);
        }

        article.Slug = request.Slug;
        article.Title = request.Title;
        article.Summary = request.Summary;
        article.Content = request.Content;
        article.CategoryId = resolved.Category?.CategoryId;
        article.Category = resolved.Category;

        article.Tags.Clear();
        article.Tags.AddRange(resolved.Tags);

        await referenceService.SetMarkdownReferencesAsync(MediaReferrerType.Article, article.ArticleId, article.Content, ct);
        await dbContext.SaveChangesAsync(ct);
        await EvictAsync(cacheStore, ct);

        return TypedResults.Ok(ToAdminDto(article));
    }

    [EndpointSummary("Delete article")]
    private static async Task<Results<NoContent, NotFound>> DeleteArticle(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var article = await dbContext.Articles.FirstOrDefaultAsync(x => x.ArticleId == id, ct);
        if (article is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Articles.Remove(article);
        await referenceService.ClearReferencesAsync(MediaReferrerType.Article, article.ArticleId, ct);
        await dbContext.SaveChangesAsync(ct);
        await EvictAsync(cacheStore, ct);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Publish article")]
    private static async Task<Results<Ok<ArticleAdminDto>, NotFound>> PublishArticle(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] TimeProvider timeProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var article = await dbContext.Articles
            .Include(x => x.Category)
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.ArticleId == id, ct);

        if (article is null)
        {
            return TypedResults.NotFound();
        }

        // Idempotent: re-publishing keeps the original date
        article.PublishedAt ??= timeProvider.GetUtcNow();

        await dbContext.SaveChangesAsync(ct);
        await EvictAsync(cacheStore, ct);

        return TypedResults.Ok(ToAdminDto(article));
    }

    [EndpointSummary("Unpublish article")]
    private static async Task<Results<Ok<ArticleAdminDto>, NotFound>> UnpublishArticle(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var article = await dbContext.Articles
            .Include(x => x.Category)
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.ArticleId == id, ct);

        if (article is null)
        {
            return TypedResults.NotFound();
        }

        article.PublishedAt = null;

        await dbContext.SaveChangesAsync(ct);
        await EvictAsync(cacheStore, ct);

        return TypedResults.Ok(ToAdminDto(article));
    }

    private sealed record ResolvedTaxonomy(Category? Category, List<Tag> Tags, ErrorResponse? Error);

    private static async Task<ResolvedTaxonomy> ResolveTaxonomyAsync(
        AtmosDbContext dbContext, ArticleRequest request, CancellationToken ct)
    {
        Category? category = null;
        if (string.IsNullOrEmpty(request.CategorySlug) is false)
        {
            category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Slug == request.CategorySlug, ct);
            if (category is null)
            {
                return new ResolvedTaxonomy(null, [], new ErrorResponse($"Category '{request.CategorySlug}' does not exist"));
            }
        }

        var tagSlugs = request.TagSlugs.Distinct().ToList();
        var tags = await dbContext.Tags.Where(x => tagSlugs.Contains(x.Slug)).ToListAsync(ct);

        if (tags.Count != tagSlugs.Count)
        {
            var missing = tagSlugs.Except(tags.Select(x => x.Slug)).ToList();
            return new ResolvedTaxonomy(null, [],
                new ErrorResponse($"Tags do not exist: {string.Join(", ", missing)}"));
        }

        return new ResolvedTaxonomy(category, tags, null);
    }

    private static async Task EvictAsync(IOutputCacheStore cacheStore, CancellationToken ct)
    {
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Articles, ct);
        // Category/tag article counts change with article mutations
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Taxonomy, ct);
    }

    private static ArticleAdminDto ToAdminDto(Article article)
    {
        return new ArticleAdminDto(
            article.ArticleId, article.Slug, article.Title, article.Summary, article.Content,
            article.Category?.Slug,
            article.Tags.Select(t => t.Slug).OrderBy(s => s).ToList(),
            article.PublishedAt, article.CreateAt, article.UpdateAt);
    }
}
