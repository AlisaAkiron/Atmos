using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Atmos.AppHost.Extensions;

public static class DistributedAppExtensions
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

    public static IResourceBuilder<IResourceWithConnectionString> AddResourceWithConnectionString(
        this IDistributedApplicationBuilder builder,
        Func<IDistributedApplicationBuilder, IResourceBuilder<IResourceWithConnectionString>> resourceBuilder,
        string connectionStringName)
    {
        var csValue = builder.Configuration.GetConnectionString(connectionStringName);
        return string.IsNullOrEmpty(csValue)
            ? resourceBuilder.Invoke(builder)
            : builder.AddConnectionString(connectionStringName);
    }

    public static IResourceBuilder<IResourceWithConnectionString> AddConnectionStringWithDefault(
        this IDistributedApplicationBuilder builder,
        string connectionStringName,
        string defaultValue)
    {
        var csValue = builder.Configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrEmpty(csValue))
        {
            builder.Configuration.AddInMemoryCollection([
                new KeyValuePair<string, string?>($"ConnectionStrings:{connectionStringName}", defaultValue)
            ]);
        }

        return builder.AddConnectionString(connectionStringName);
    }
}
