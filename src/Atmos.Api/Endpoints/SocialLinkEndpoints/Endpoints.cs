using Atmos.Api.Endpoints.Dto;
using Atmos.Database;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Media.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Api.Endpoints;

public partial class SocialLinkEndpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/social-links")
            .HasApiVersion(1)
            .WithTags("SocialLinks");

        publicGroup.MapGet("/", GetSocialLinks).CacheOutput(ContentCaching.Policies.SocialLinks);

        MapAdminEndpoints(endpoints);
    }

    [EndpointSummary("List social links")]
    private static async Task<Ok<List<SocialLinkDto>>> GetSocialLinks(
        [FromServices] AtmosDbContext dbContext,
        [FromServices] MediaUrlBuilder urlBuilder,
        CancellationToken ct)
    {
        var links = await dbContext.SocialLinks
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new { x.Url, x.Label, x.Color, x.Invert, IconKey = x.Icon.Key })
            .ToListAsync(ct);

        var result = links
            .Select(x => new SocialLinkDto(x.Url, x.Label, x.Color, x.Invert, urlBuilder.GetPublicUrl(x.IconKey)))
            .ToList();

        return TypedResults.Ok(result);
    }
}
