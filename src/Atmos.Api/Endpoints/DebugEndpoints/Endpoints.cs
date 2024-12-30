using System.Globalization;
using System.Text;
using Atmos.Services.Api.Abstract;

namespace Atmos.Api.Endpoints.DebugEndpoints;

public class Endpoints : IEndpointMapper
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var debugGroup = endpoints.MapGroup("/debug")
            .HasApiVersion(1)
            .WithTags("Debug");

        debugGroup.MapGet("/auth/userinfo", (HttpContext ctx) =>
        {
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

                foreach (var claim in identity.Claims)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  {claim.Type}: {claim.Value}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        });
    }
}
