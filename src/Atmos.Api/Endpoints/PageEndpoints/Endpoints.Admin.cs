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

public partial class PageEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/admin/pages")
            .HasApiVersion(1)
            .WithTags("Pages Admin")
            .RequireAuthorization(AtmosAuthenticationDefaults.SiteOwnerPolicy);

        adminGroup.MapGet("/", GetPagesAdmin);
        adminGroup.MapGet("/{id:guid}", GetPageAdmin);
        adminGroup.MapPost("/", CreatePage);
        adminGroup.MapPut("/{id:guid}", UpdatePage);
        adminGroup.MapDelete("/{id:guid}", DeletePage);
        adminGroup.MapPost("/{id:guid}/publish", PublishPage);
        adminGroup.MapPost("/{id:guid}/unpublish", UnpublishPage);
    }

    [EndpointSummary("List pages (admin)")]
    [EndpointDescription(
        "Returns pages by slug, draft and published alike, bodies included. A page below 1 is clamped, and pageSize " +
        "is clamped to the server maximum."
    )]
    private static async Task<Ok<PageAdminListResponse>> GetPagesAdmin(
        [FromServices] AtmosDbContext dbContext,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "pageSize")] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await dbContext.Pages.CountAsync(ct);

        // Bodies are included, so an unpaginated listing grows with the site
        var pages = await dbContext.Pages
            .OrderBy(x => x.Slug)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PageAdminDto(x.PageId, x.Slug, x.Title, x.Content, x.PublishedAt, x.CreateAt, x.UpdateAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PageAdminListResponse(totalCount, page, pageSize, pages));
    }

    [EndpointSummary("Get page (admin)")]
    [EndpointDescription("Returns one page by ID, including its body and its draft state.")]
    private static async Task<Results<Ok<PageAdminDto>, NotFound>> GetPageAdmin(
        [FromServices] AtmosDbContext dbContext,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var page = await dbContext.Pages
            .Where(x => x.PageId == id)
            .Select(x => new PageAdminDto(x.PageId, x.Slug, x.Title, x.Content, x.PublishedAt, x.CreateAt, x.UpdateAt))
            .FirstOrDefaultAsync(ct);

        return page is null ? TypedResults.NotFound() : TypedResults.Ok(page);
    }

    [EndpointSummary("Create page")]
    [EndpointDescription("Creates a page as a draft. The slug must be well formed and not already taken.")]
    private static async Task<Results<Created<PageAdminDto>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreatePage(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IGuidProvider guidProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromBody] PageRequest request,
        CancellationToken ct)
    {
        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var exists = await dbContext.Pages.AnyAsync(x => x.Slug == request.Slug, ct);
        if (exists)
        {
            return TypedResults.Conflict(new ErrorResponse($"Page slug '{request.Slug}' already exists"));
        }

        var page = new Page
        {
            PageId = guidProvider.Create(),
            Slug = request.Slug,
            Title = request.Title,
            Content = request.Content
        };

        await dbContext.Pages.AddAsync(page, ct);
        await referenceService.SetMarkdownReferencesAsync(MediaReferrerType.Page, page.PageId, page.Content, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Pages, ct);

        return TypedResults.Created($"/api/admin/pages/{page.PageId}", ToAdminDto(page));
    }

    [EndpointSummary("Update page")]
    [EndpointDescription(
        "Replaces a page's content. The same slug rules as creation apply; publication state is left untouched."
    )]
    private static async Task<Results<Ok<PageAdminDto>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> UpdatePage(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        [FromBody] PageRequest request,
        CancellationToken ct)
    {
        var page = await dbContext.Pages.FirstOrDefaultAsync(x => x.PageId == id, ct);
        if (page is null)
        {
            return TypedResults.NotFound();
        }

        if (SlugUtils.IsValid(request.Slug) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid slug '{request.Slug}'"));
        }

        var slugTaken = await dbContext.Pages.AnyAsync(x => x.Slug == request.Slug && x.PageId != id, ct);
        if (slugTaken)
        {
            return TypedResults.Conflict(new ErrorResponse($"Page slug '{request.Slug}' already exists"));
        }

        page.Slug = request.Slug;
        page.Title = request.Title;
        page.Content = request.Content;

        await referenceService.SetMarkdownReferencesAsync(MediaReferrerType.Page, page.PageId, page.Content, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Pages, ct);

        return TypedResults.Ok(ToAdminDto(page));
    }

    [EndpointSummary("Delete page")]
    [EndpointDescription("Deletes a page outright. Media it referenced is released, not deleted.")]
    private static async Task<Results<NoContent, NotFound>> DeletePage(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var page = await dbContext.Pages.FirstOrDefaultAsync(x => x.PageId == id, ct);
        if (page is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Pages.Remove(page);
        await referenceService.ClearReferencesAsync(MediaReferrerType.Page, page.PageId, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Pages, ct);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Publish page")]
    [EndpointDescription("Publishes a draft page, stamping its publication time.")]
    private static async Task<Results<Ok<PageAdminDto>, NotFound>> PublishPage(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] TimeProvider timeProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var page = await dbContext.Pages.FirstOrDefaultAsync(x => x.PageId == id, ct);
        if (page is null)
        {
            return TypedResults.NotFound();
        }

        // Idempotent: re-publishing keeps the original date
        page.PublishedAt ??= timeProvider.GetUtcNow();

        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Pages, ct);

        return TypedResults.Ok(ToAdminDto(page));
    }

    [EndpointSummary("Unpublish page")]
    [EndpointDescription("Returns a page to draft, clearing its publication time and taking it off the public route.")]
    private static async Task<Results<Ok<PageAdminDto>, NotFound>> UnpublishPage(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var page = await dbContext.Pages.FirstOrDefaultAsync(x => x.PageId == id, ct);
        if (page is null)
        {
            return TypedResults.NotFound();
        }

        page.PublishedAt = null;

        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.Pages, ct);

        return TypedResults.Ok(ToAdminDto(page));
    }

    private static PageAdminDto ToAdminDto(Page page)
    {
        return new PageAdminDto(page.PageId, page.Slug, page.Title, page.Content,
            page.PublishedAt, page.CreateAt, page.UpdateAt);
    }
}
