using Atmos.Services.Api.Abstract;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Atmos.Api.Content.Endpoints;

public class HelloWorldEndpoints : IEndpointMapper
{
    /// <inheritdoc />
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var hello = endpoints.MapGroup("/hello");

        hello.MapGet("/{message}",
            Results<Ok<string>, BadRequest<string>>
            (string message) =>
        {
            if (message == "error")
            {
                return TypedResults.BadRequest("error");
            }

            return TypedResults.Ok($"Hello {message}");
        });
    }
}
