using System.Security.Cryptography;
using Atmos.Api.Endpoints.Dto;
using Atmos.Common.Abstract;
using Atmos.Common.Utils;
using Atmos.Database;
using Atmos.Services.Api;
using Atmos.Services.Api.Models;
using Atmos.Services.Media.Abstract;
using Atmos.Services.Media.Options;
using Atmos.Services.Media.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Atmos.Api.Endpoints;

public partial class MediaEndpoints
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/admin/media")
            .HasApiVersion(1)
            .WithTags("Media Admin")
            .RequireAuthorization(AtmosAuthenticationDefaults.SiteOwnerPolicy);

        adminGroup.MapGet("/", GetMediaList);
        adminGroup.MapDelete("/{id:guid}", DeleteMedia);

        // Cross-site POSTs never carry the SameSite=Lax auth cookie, so the
        // form endpoint does not need an antiforgery token.
        adminGroup.MapPost("/", UploadMedia)
            .DisableAntiforgery()
            .AddEndpointFilter(async (context, next) =>
            {
                // Kestrel's default body limit (30 MB) is below the configured upload cap
                var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<MediaOptions>>().Value;
                var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (feature is not null && feature.IsReadOnly is false)
                {
                    feature.MaxRequestBodySize = options.MaxUploadSizeBytes;
                }

                return await next(context);
            });
    }

    [EndpointSummary("Upload media")]
    private static async Task<Results<Created<MediaUploadResponse>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>, ProblemHttpResult>> UploadMedia(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IMediaStorage storage,
        [FromServices] MediaUrlBuilder urlBuilder,
        [FromServices] IGuidProvider guidProvider,
        IFormFile file,
        [FromForm(Name = "key")] string key,
        CancellationToken ct)
    {
        if (MediaKeyUtils.IsValid(key) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse($"Invalid media key '{key}'"));
        }

        var keyTaken = await dbContext.Media.AnyAsync(x => x.Key == key, ct);
        if (keyTaken)
        {
            return TypedResults.Conflict(new ErrorResponse($"Media key '{key}' already exists"));
        }

        string sha256;
        await using (var hashStream = file.OpenReadStream())
        {
            sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(hashStream, ct));
        }

        var duplicateOfKey = await dbContext.Media
            .Where(x => x.Sha256 == sha256)
            .Select(x => x.Key)
            .FirstOrDefaultAsync(ct);

        var contentType = ResolveContentType(key, file.ContentType);

        // Bucket write first: if it fails, no DB row is created
        try
        {
            await using var uploadStream = file.OpenReadStream();
            await storage.UploadAsync(key, uploadStream, contentType, ct);
        }
        catch (Exception e) when (e is Amazon.S3.AmazonS3Exception or IOException or UnauthorizedAccessException)
        {
            // Same 502 for both providers: from the client's side the storage layer
            // failed, whether that is R2 rejecting the write or a full local disk.
            // UnauthorizedAccessException does not derive from IOException, so it
            // needs naming explicitly.
            return TypedResults.Problem($"Media storage rejected the upload: {e.Message}",
                statusCode: StatusCodes.Status502BadGateway);
        }

        var media = new Atmos.Domain.Entities.MediaStorage.Media
        {
            MediaId = guidProvider.Create(),
            Key = key,
            ContentType = contentType,
            SizeBytes = file.Length,
            Sha256 = sha256
        };

        await dbContext.Media.AddAsync(media, ct);
        await dbContext.SaveChangesAsync(ct);

        var response = new MediaUploadResponse(media.MediaId, media.Key, urlBuilder.GetPublicUrl(media.Key),
            media.ContentType, media.SizeBytes, media.Sha256, duplicateOfKey);

        return TypedResults.Created($"/api/admin/media/{media.MediaId}", response);
    }

    [EndpointSummary("List media")]
    private static async Task<Ok<List<MediaAdminDto>>> GetMediaList(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        [FromQuery(Name = "prefix")] string? prefix,
        [FromQuery(Name = "unreferenced")] bool unreferenced = false,
        CancellationToken ct = default)
    {
        var query = dbContext.Media.AsQueryable();

        if (string.IsNullOrEmpty(prefix) is false)
        {
            query = query.Where(x => x.Key.StartsWith(prefix));
        }

        if (unreferenced)
        {
            query = query.Where(x => x.References.Count == 0);
        }

        var items = await query
            .OrderBy(x => x.Key)
            .Select(x => new
            {
                x.MediaId,
                x.Key,
                x.ContentType,
                x.SizeBytes,
                x.Sha256,
                References = x.References
                    .Select(r => new { r.ReferrerType, r.ReferrerId })
                    .ToList()
            })
            .ToListAsync(ct);

        var result = items
            .Select(x => new MediaAdminDto(
                x.MediaId, x.Key, urlBuilder.GetPublicUrl(x.Key), x.ContentType, x.SizeBytes, x.Sha256,
                x.References.Count,
                x.References
                    .Select(r => new MediaReferrerDto(r.ReferrerType.ToString(), r.ReferrerId))
                    .ToList()))
            .ToList();

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Delete media")]
    private static async Task<Results<NoContent, NotFound, Conflict<MediaAdminDto>, ProblemHttpResult>> DeleteMedia(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] IMediaStorage storage,
        [FromServices] MediaUrlBuilder urlBuilder,
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var media = await dbContext.Media
            .Include(x => x.References)
            .FirstOrDefaultAsync(x => x.MediaId == id, ct);

        if (media is null)
        {
            return TypedResults.NotFound();
        }

        if (media.References.Count > 0)
        {
            // The conflict body shows exactly what still references this media
            var dto = new MediaAdminDto(
                media.MediaId, media.Key, urlBuilder.GetPublicUrl(media.Key), media.ContentType,
                media.SizeBytes, media.Sha256, media.References.Count,
                media.References
                    .Select(r => new MediaReferrerDto(r.ReferrerType.ToString(), r.ReferrerId))
                    .ToList());

            return TypedResults.Conflict(dto);
        }

        // Bucket delete first: if it fails, the row (and the cleanup view) survives
        try
        {
            await storage.DeleteAsync(media.Key, ct);
        }
        catch (Exception e) when (e is Amazon.S3.AmazonS3Exception or IOException or UnauthorizedAccessException)
        {
            return TypedResults.Problem($"Media storage rejected the delete: {e.Message}",
                statusCode: StatusCodes.Status502BadGateway);
        }

        dbContext.Media.Remove(media);
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static string ResolveContentType(string key, string? clientContentType)
    {
        if (ContentTypeProvider.TryGetContentType(key, out var mapped))
        {
            return mapped;
        }

        return string.IsNullOrEmpty(clientContentType) ? "application/octet-stream" : clientContentType;
    }
}
