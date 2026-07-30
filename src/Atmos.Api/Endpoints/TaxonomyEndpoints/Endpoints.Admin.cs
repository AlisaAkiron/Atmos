using Atmos.Api.Endpoints.Dto;
using Atmos.Common.Abstract;
using Atmos.Common.Utils;
using Atmos.Database;
using Atmos.Domain.Entities.Content;
using Atmos.Services.Api;
using Atmos.Services.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints;

public partial class TaxonomyEndpoints
{
    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var adminCategories = endpoints.MapGroup("/admin/categories")
            .HasApiVersion(1)
            .WithTags("Taxonomy Admin")
            .RequireAuthorization(AtmosAuthenticationDefaults.SiteOwnerPolicy);

        adminCategories.MapGet("/", GetCategoriesAdmin);
        adminCategories.MapPost("/", CreateCategory);
        adminCategories.MapPut("/{id:guid}", UpdateCategory);
        adminCategories.MapDelete("/{id:guid}", DeleteCategory);

        var adminTags = endpoints.MapGroup("/admin/tags")
            .HasApiVersion(1)
            .WithTags("Taxonomy Admin")
            .RequireAuthorization(AtmosAuthenticationDefaults.SiteOwnerPolicy);

        adminTags.MapGet("/", GetTagsAdmin);
        adminTags.MapPost("/", CreateTag);
        adminTags.MapPut("/{id:guid}", UpdateTag);
        adminTags.MapDelete("/{id:guid}", DeleteTag);
    }

    [EndpointSummary("List categories (admin)")]
    private static async Task<Ok<List<CategoryAdminDto>>> GetCategoriesAdmin(
        [FromServices] AtmosDbContext dbContext,
        CancellationToken ct)
    {
        var result = await dbContext.Categories
            .OrderBy(x => x.Name)
            .Select(x => new CategoryAdminDto(x.CategoryId, x.Slug, x.Name, x.Description, x.Articles.Count))
            .ToListAsync(ct);

        return TypedResults.Ok(result);
    }

    [EndpointSummary("List tags (admin)")]
    private static async Task<Ok<List<TagAdminDto>>> GetTagsAdmin(
        [FromServices] AtmosDbContext dbContext,
        CancellationToken ct)
    {
        var result = await dbContext.Tags
            .OrderBy(x => x.Name)
            .Select(x => new TagAdminDto(x.TagId, x.Slug, x.Name, x.Articles.Count))
            .ToListAsync(ct);

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create category")]
    private static async Task<Results<Created<CategoryAdminDto>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateCategory(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IGuidProvider guidProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromBody] CategoryRequest request,
        CancellationToken ct)
    {
        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var exists = await dbContext.Categories.AnyAsync(x => x.Slug == request.Slug, ct);
        if (exists)
        {
            return TypedResults.Conflict(new ErrorResponse($"Category slug '{request.Slug}' already exists"));
        }

        var category = new Category
        {
            CategoryId = guidProvider.Create(),
            Slug = request.Slug,
            Name = request.Name,
            Description = request.Description
        };

        await dbContext.Categories.AddAsync(category, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Taxonomy, ct);

        var dto = new CategoryAdminDto(category.CategoryId, category.Slug, category.Name, category.Description, 0);
        return TypedResults.Created($"/api/admin/categories/{category.CategoryId}", dto);
    }

    [EndpointSummary("Update category")]
    private static async Task<Results<Ok<CategoryAdminDto>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> UpdateCategory(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        [FromBody] CategoryRequest request,
        CancellationToken ct)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.CategoryId == id, ct);
        if (category is null)
        {
            return TypedResults.NotFound();
        }

        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var slugTaken = await dbContext.Categories.AnyAsync(x => x.Slug == request.Slug && x.CategoryId != id, ct);
        if (slugTaken)
        {
            return TypedResults.Conflict(new ErrorResponse($"Category slug '{request.Slug}' already exists"));
        }

        category.Slug = request.Slug;
        category.Name = request.Name;
        category.Description = request.Description;

        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Taxonomy, ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Articles, ct);

        var count = await dbContext.Articles.CountAsync(x => x.CategoryId == id, ct);
        return TypedResults.Ok(new CategoryAdminDto(category.CategoryId, category.Slug, category.Name, category.Description, count));
    }

    [EndpointSummary("Delete category")]
    private static async Task<Results<NoContent, NotFound>> DeleteCategory(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.CategoryId == id, ct);
        if (category is null)
        {
            return TypedResults.NotFound();
        }

        // FK is ON DELETE SET NULL: articles in this category become uncategorized
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Taxonomy, ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Articles, ct);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Create tag")]
    private static async Task<Results<Created<TagAdminDto>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateTag(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IGuidProvider guidProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromBody] TagRequest request,
        CancellationToken ct)
    {
        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var exists = await dbContext.Tags.AnyAsync(x => x.Slug == request.Slug, ct);
        if (exists)
        {
            return TypedResults.Conflict(new ErrorResponse($"Tag slug '{request.Slug}' already exists"));
        }

        var tag = new Tag
        {
            TagId = guidProvider.Create(),
            Slug = request.Slug,
            Name = request.Name
        };

        await dbContext.Tags.AddAsync(tag, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Taxonomy, ct);

        return TypedResults.Created($"/api/admin/tags/{tag.TagId}", new TagAdminDto(tag.TagId, tag.Slug, tag.Name, 0));
    }

    [EndpointSummary("Update tag")]
    private static async Task<Results<Ok<TagAdminDto>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> UpdateTag(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        [FromBody] TagRequest request,
        CancellationToken ct)
    {
        var tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.TagId == id, ct);
        if (tag is null)
        {
            return TypedResults.NotFound();
        }

        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var slugTaken = await dbContext.Tags.AnyAsync(x => x.Slug == request.Slug && x.TagId != id, ct);
        if (slugTaken)
        {
            return TypedResults.Conflict(new ErrorResponse($"Tag slug '{request.Slug}' already exists"));
        }

        tag.Slug = request.Slug;
        tag.Name = request.Name;

        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Taxonomy, ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Articles, ct);

        var count = await dbContext.Tags
            .Where(x => x.TagId == id)
            .Select(x => x.Articles.Count)
            .FirstAsync(ct);

        return TypedResults.Ok(new TagAdminDto(tag.TagId, tag.Slug, tag.Name, count));
    }

    [EndpointSummary("Delete tag")]
    private static async Task<Results<NoContent, NotFound>> DeleteTag(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.TagId == id, ct);
        if (tag is null)
        {
            return TypedResults.NotFound();
        }

        // Join rows in article_tag cascade-delete; articles themselves are untouched
        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Taxonomy, ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Articles, ct);

        return TypedResults.NoContent();
    }
}
