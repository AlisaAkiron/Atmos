using Atmos.Services.Api.Components;
using Microsoft.Extensions.Hosting;

namespace Atmos.Services.Api;

public static class Extensions
{
    public static IHostApplicationBuilder AddAtmosApiServices(this IHostApplicationBuilder builder)
    {
        builder.ConfigureNpgsql();

        return builder;
    }
}
