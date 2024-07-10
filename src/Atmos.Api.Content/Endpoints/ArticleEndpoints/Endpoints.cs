using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Models;

namespace Atmos.Api.Content.Endpoints.ArticleEndpoints;

public class ArticleEndpoints : IEndpointMapper
{
    /// <inheritdoc />
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var articleGroup = endpoints.MapGroup("/article")
            .HasApiVersion(1)
            .WithTags("Article");

        // GET single
        articleGroup.MapGet("/{slug}", (string slug) =>
        {
            return slug switch
            {
                "error" => new ErrorResponse("error").BadRequest(),
                "empty" => Results.BadRequest(),
                "p" => Results.Problem("failed"),
                "exception" => throw new InvalidOperationException("EXCEPTION"),
                _ => Results.Ok(slug)
            };
        })
        .WithDescription("Get a single article by slug, including content");

        // GET all
        articleGroup.MapGet("/", () => "all")
            .WithDescription("Get all articles");
    }
}
