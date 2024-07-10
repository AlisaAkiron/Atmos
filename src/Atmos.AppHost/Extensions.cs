using System.Globalization;
using Atmos.AppHost.Model;
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

    public static ComponentVersion GetComponentVersion(this IDistributedApplicationBuilder builder)
    {
        // AddParameter does not accept a default value, which we should provide
        // This feature is not yet implemented and will be available in a future release (Aspire 8.1)
        // Issue: https://github.com/dotnet/aspire/issues/4429
        var postgreSqlTag = builder.AddParameter("postgresql-tag");
        var pgAdminTag = builder.AddParameter("pgadmin-tag");
        var redisTag = builder.AddParameter("redis-tag");

        return new ComponentVersion
        {
            PostgreSqlTag = postgreSqlTag.Resource.Value,
            PgAdminTag = pgAdminTag.Resource.Value,
            RedisTag = redisTag.Resource.Value
        };
    }
}
