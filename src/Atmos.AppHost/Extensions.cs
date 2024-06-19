using System.Globalization;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Atmos.AppHost;

public static class Extensions
{
    public static IDistributedApplicationBuilder AddAtmosAppHost(this IDistributedApplicationBuilder builder)
    {
        builder.ConfigureSerilog();

        return builder;
    }

    private static IDistributedApplicationBuilder ConfigureSerilog(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddSerilog(cfg =>
        {
            cfg
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);

            if (builder.Environment.IsProduction())
            {
                cfg.MinimumLevel.Override("Aspire.Hosting.Dcp", LogEventLevel.Warning);
            }
        });

        return builder;
    }
}
