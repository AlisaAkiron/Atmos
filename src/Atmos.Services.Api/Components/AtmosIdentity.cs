using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atmos.Services.Api.Components;

public static class AtmosIdentity
{
    internal static IHostApplicationBuilder ConfigureIdentity(this IHostApplicationBuilder builder)
    {
        const string defaultScheme = "Bearer";

        var authenticationBuilder = builder.Services.AddAuthentication(defaultScheme);

        if (builder.Environment.IsDevelopment())
        {
            authenticationBuilder.AddJwtBearer(defaultScheme, options =>
            {
            });
        }

        return builder;
    }
}
