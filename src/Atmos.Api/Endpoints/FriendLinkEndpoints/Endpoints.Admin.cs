using Atmos.Api.Endpoints.Dto;
using Atmos.Common.Abstract;
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

public partial class FriendLinkEndpoints
{
    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/admin/friend-links")
            .HasApiVersion(1)
            .WithTags("FriendLinks Admin")
            .RequireAuthorization(AtmosAuthenticationDefaults.SiteOwnerPolicy);

        adminGroup.MapGet("/", GetFriendLinksAdmin);
        adminGroup.MapPost("/", CreateFriendLink);
        adminGroup.MapPut("/{id:guid}", UpdateFriendLink);
        adminGroup.MapDelete("/{id:guid}", DeleteFriendLink);
    }

    [EndpointSummary("List friend links (admin)")]
    private static async Task<Ok<List<FriendLinkAdminDto>>> GetFriendLinksAdmin(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        CancellationToken ct)
    {
        var links = await dbContext.FriendLinks
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new { Entity = x, AvatarKey = x.Avatar != null ? x.Avatar.Key : null })
            .ToListAsync(ct);

        var result = links
            .Select(x => ToAdminDto(x.Entity, x.AvatarKey is null ? null : urlBuilder.GetPublicUrl(x.AvatarKey)))
            .ToList();

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create friend link")]
    private static async Task<Results<Created<FriendLinkAdminDto>, BadRequest<ErrorResponse>>> CreateFriendLink(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IGuidProvider guidProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromBody] FriendLinkRequest request,
        CancellationToken ct)
    {
        var avatarKey = await ResolveAvatarKeyAsync(dbContext, request.AvatarMediaId, ct);
        if (request.AvatarMediaId.HasValue && avatarKey is null)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Avatar media '{request.AvatarMediaId}' does not exist"));
        }

        var link = new FriendLink
        {
            FriendLinkId = guidProvider.Create(),
            Url = request.Url,
            Title = request.Title,
            Description = request.Description,
            AvatarMediaId = request.AvatarMediaId,
            DisplayOrder = request.DisplayOrder
        };

        await dbContext.FriendLinks.AddAsync(link, ct);

        List<Guid> mediaIds = request.AvatarMediaId.HasValue ? [request.AvatarMediaId.Value] : [];
        await referenceService.SetReferencesAsync(MediaReferrerType.FriendLink, link.FriendLinkId, mediaIds, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.FriendLinks, ct);

        var dto = ToAdminDto(link, avatarKey is null ? null : urlBuilder.GetPublicUrl(avatarKey));
        return TypedResults.Created($"/api/admin/friend-links/{link.FriendLinkId}", dto);
    }

    [EndpointSummary("Update friend link")]
    private static async Task<Results<Ok<FriendLinkAdminDto>, NotFound, BadRequest<ErrorResponse>>> UpdateFriendLink(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        [FromBody] FriendLinkRequest request,
        CancellationToken ct)
    {
        var link = await dbContext.FriendLinks.FirstOrDefaultAsync(x => x.FriendLinkId == id, ct);
        if (link is null)
        {
            return TypedResults.NotFound();
        }

        var avatarKey = await ResolveAvatarKeyAsync(dbContext, request.AvatarMediaId, ct);
        if (request.AvatarMediaId.HasValue && avatarKey is null)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Avatar media '{request.AvatarMediaId}' does not exist"));
        }

        link.Url = request.Url;
        link.Title = request.Title;
        link.Description = request.Description;
        link.AvatarMediaId = request.AvatarMediaId;
        link.DisplayOrder = request.DisplayOrder;

        List<Guid> mediaIds = request.AvatarMediaId.HasValue ? [request.AvatarMediaId.Value] : [];
        await referenceService.SetReferencesAsync(MediaReferrerType.FriendLink, link.FriendLinkId, mediaIds, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.FriendLinks, ct);

        return TypedResults.Ok(ToAdminDto(link, avatarKey is null ? null : urlBuilder.GetPublicUrl(avatarKey)));
    }

    [EndpointSummary("Delete friend link")]
    private static async Task<Results<NoContent, NotFound>> DeleteFriendLink(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var link = await dbContext.FriendLinks.FirstOrDefaultAsync(x => x.FriendLinkId == id, ct);
        if (link is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.FriendLinks.Remove(link);
        await referenceService.ClearReferencesAsync(MediaReferrerType.FriendLink, link.FriendLinkId, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.FriendLinks, ct);

        return TypedResults.NoContent();
    }

    private static async Task<string?> ResolveAvatarKeyAsync(AtmosDbContext dbContext, Guid? avatarMediaId, CancellationToken ct)
    {
        if (avatarMediaId.HasValue is false)
        {
            return null;
        }

        return await dbContext.Media
            .Where(x => x.MediaId == avatarMediaId.Value)
            .Select(x => x.Key)
            .FirstOrDefaultAsync(ct);
    }

    private static FriendLinkAdminDto ToAdminDto(FriendLink link, string? avatarUrl)
    {
        return new FriendLinkAdminDto(link.FriendLinkId, link.Url, link.Title, link.Description,
            link.AvatarMediaId, avatarUrl, link.DisplayOrder);
    }
}
