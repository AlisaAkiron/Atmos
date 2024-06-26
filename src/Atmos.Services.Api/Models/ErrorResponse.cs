using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Atmos.Services.Api.Models;

public record ErrorResponse
{
    public ErrorResponse(string message)
    {
        Message = message;
    }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public IResult InternalServerError()
    {
        return Results.Problem(
            detail: this.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }

    public IResult BadRequest()
    {
        return Results.BadRequest(this);
    }
}
