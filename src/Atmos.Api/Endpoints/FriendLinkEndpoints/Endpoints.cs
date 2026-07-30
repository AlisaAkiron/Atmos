using Atmos.Api.Endpoints.Dto;
using Atmos.Database;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Media.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints;

public partial class FriendLinkEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/friend-links")
            .HasApiVersion(1)
            .WithTags("FriendLinks");

        publicGroup.MapGet("/", GetFriendLinks).CacheOutput(ContentCaching.Policies.FriendLinks);

        MapAdminEndpoints(endpoints);
    }

    [EndpointSummary("List friend links")]
    private static async Task<Ok<List<FriendLinkDto>>> GetFriendLinks(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        CancellationToken ct)
    {
        var links = await dbContext.FriendLinks
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new { x.Url, x.Title, x.Description, AvatarKey = x.Avatar != null ? x.Avatar.Key : null })
            .ToListAsync(ct);

        var result = links
            .Select(x => new FriendLinkDto(x.Url, x.Title, x.Description,
                x.AvatarKey is null ? null : urlBuilder.GetPublicUrl(x.AvatarKey)))
            .ToList();

        return TypedResults.Ok(result);
    }
}
