using Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints;

public partial class Endpoints
{
    [EndpointSummary("Send magic link to email")]
    private static async Task<NoContent> SendLinkAsync(
        [FromBody] MagicLinkSendDto dto)
    {
        await Task.Delay(1000);
        return TypedResults.NoContent();
    }
}
