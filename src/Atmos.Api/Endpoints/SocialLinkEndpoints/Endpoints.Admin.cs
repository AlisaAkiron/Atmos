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

public partial class SocialLinkEndpoints
{
    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/admin/social-links")
            .HasApiVersion(1)
            .WithTags("SocialLinks Admin")
            .RequireAuthorization(AtmosAuthenticationDefaults.SiteOwnerPolicy);

        adminGroup.MapGet("/", GetSocialLinksAdmin);
        adminGroup.MapPost("/", CreateSocialLink);
        adminGroup.MapPut("/{id:guid}", UpdateSocialLink);
        adminGroup.MapDelete("/{id:guid}", DeleteSocialLink);
    }

    [EndpointSummary("List social links (admin)")]
    private static async Task<Ok<List<SocialLinkAdminDto>>> GetSocialLinksAdmin(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        CancellationToken ct)
    {
        var links = await dbContext.SocialLinks
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new { Entity = x, IconKey = x.Icon.Key })
            .ToListAsync(ct);

        var result = links.Select(x => ToAdminDto(x.Entity, urlBuilder.GetPublicUrl(x.IconKey))).ToList();

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create social link")]
    private static async Task<Results<Created<SocialLinkAdminDto>, BadRequest<ErrorResponse>>> CreateSocialLink(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IGuidProvider guidProvider,
        [FromServices] IOutputCacheStore cacheStore,
        [FromBody] SocialLinkRequest request,
        CancellationToken ct)
    {
        var icon = await dbContext.Media.FirstOrDefaultAsync(x => x.MediaId == request.IconMediaId, ct);
        if (icon is null)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Icon media '{request.IconMediaId}' does not exist"));
        }

        var link = new SocialLink
        {
            SocialLinkId = guidProvider.Create(),
            Url = request.Url,
            Label = request.Label,
            Color = request.Color,
            Invert = request.Invert,
            IconMediaId = request.IconMediaId,
            DisplayOrder = request.DisplayOrder
        };

        await dbContext.SocialLinks.AddAsync(link, ct);
        await referenceService.SetReferencesAsync(MediaReferrerType.SocialLink, link.SocialLinkId, [request.IconMediaId], ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.SocialLinks, ct);

        var dto = ToAdminDto(link, urlBuilder.GetPublicUrl(icon.Key));
        return TypedResults.Created($"/api/admin/social-links/{link.SocialLinkId}", dto);
    }

    [EndpointSummary("Update social link")]
    private static async Task<Results<Ok<SocialLinkAdminDto>, NotFound, BadRequest<ErrorResponse>>> UpdateSocialLink(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        [FromBody] SocialLinkRequest request,
        CancellationToken ct)
    {
        var link = await dbContext.SocialLinks.FirstOrDefaultAsync(x => x.SocialLinkId == id, ct);
        if (link is null)
        {
            return TypedResults.NotFound();
        }

        var icon = await dbContext.Media.FirstOrDefaultAsync(x => x.MediaId == request.IconMediaId, ct);
        if (icon is null)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Icon media '{request.IconMediaId}' does not exist"));
        }

        link.Url = request.Url;
        link.Label = request.Label;
        link.Color = request.Color;
        link.Invert = request.Invert;
        link.IconMediaId = request.IconMediaId;
        link.DisplayOrder = request.DisplayOrder;

        await referenceService.SetReferencesAsync(MediaReferrerType.SocialLink, link.SocialLinkId, [request.IconMediaId], ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.SocialLinks, ct);

        return TypedResults.Ok(ToAdminDto(link, urlBuilder.GetPublicUrl(icon.Key)));
    }

    [EndpointSummary("Delete social link")]
    private static async Task<Results<NoContent, NotFound>> DeleteSocialLink(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaReferenceService referenceService,
        [FromServices] IOutputCacheStore cacheStore,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var link = await dbContext.SocialLinks.FirstOrDefaultAsync(x => x.SocialLinkId == id, ct);
        if (link is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.SocialLinks.Remove(link);
        await referenceService.ClearReferencesAsync(MediaReferrerType.SocialLink, link.SocialLinkId, ct);
        await dbContext.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync(ContentCaching.Tags.SocialLinks, ct);

        return TypedResults.NoContent();
    }

    private static SocialLinkAdminDto ToAdminDto(SocialLink link, string iconUrl)
    {
        return new SocialLinkAdminDto(link.SocialLinkId, link.Url, link.Label, link.Color, link.Invert,
            link.IconMediaId, iconUrl, link.DisplayOrder);
    }
}
