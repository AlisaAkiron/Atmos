using System.Globalization;
using System.Security.Claims;
using System.Text;
using Atmos.Services.Api.Abstract;
using Atmos.Templates;
using Atmos.Templates.Components.Emails;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Atmos.Api.Endpoints.DebugEndpoints;

public class Endpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var debugGroup = endpoints.MapGroup("/debug")
            .HasApiVersion(1)
            .WithTags("Debug");

        debugGroup.MapGet("/auth/userinfo", GetDebugUserInfo);
        debugGroup.MapGet("/email/render/{viewName}", GetDebugRenderEmail);
    }

    private static Ok<string> GetDebugUserInfo(
        [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null)
        {
            return TypedResults.Ok("HttpContext is null");
        }
        var user = ctx.User;

        var identities = user.Identities.Select(x => new
        {
            x.AuthenticationType,
            x.Name,
            x.IsAuthenticated,
            Claims = x.Claims.Select(y => new
            {
                y.Type,
                y.Value
            })
        });

        var sb = new StringBuilder();

        foreach (var identity in identities)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"AuthenticationType: {identity.AuthenticationType}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Name: {identity.Name}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"IsAuthenticated: {identity.IsAuthenticated}");

            sb.AppendLine(CultureInfo.InvariantCulture, $"Claims:");
            foreach (var claim in identity.Claims)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {claim.Type}: {claim.Value}");
            }

            sb.AppendLine();

            sb.AppendLine(CultureInfo.InvariantCulture, $"Mapped User Info:");

            var email = user.FindFirstValue(ClaimTypes.Email);
            var name = user.FindFirstValue(ClaimTypes.Name);
            var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);

            sb.AppendLine(CultureInfo.InvariantCulture, $"  Email: {email}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Name: {name}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Sub: {sub}");

            sb.AppendLine();
        }

        return TypedResults.Ok(sb.ToString());
    }

    private static async Task<Ok<string>> GetDebugRenderEmail(
        [FromServices] TemplateRenderer templateRenderer,
        [FromRoute(Name = "viewName")] string viewName)
    {
        switch (viewName)
        {
            case "MagicLink":
            {
                var result = await templateRenderer.RenderTemplateAsync<MagicLinkComposite, MagicLinkViewModel>(new MagicLinkViewModel
                {
                    Token = "123456",
                    Link = "https://example.com/magiclink",
                    ExpirationMinutes = 10
                });
                return TypedResults.Ok(result);
            }
        }

        return TypedResults.Ok("No view found");
    }
}
