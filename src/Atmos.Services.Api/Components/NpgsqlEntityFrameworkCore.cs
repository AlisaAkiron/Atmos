using Atmos.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Atmos.Services.Api.Components;

public static class NpgsqlEntityFrameworkCore
{
    public static IHostApplicationBuilder ConfigureNpgsql(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");

        builder.Services.AddDbContext<AtmosDbContext>(options =>
        {
            options.UseNpgsql(connectionString);

            if (builder.Environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        builder.Services
            .AddHealthChecks()
            .AddDbContextCheck<AtmosDbContext>("npgsql");

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddNpgsqlInstrumentation();
            })
            .WithTracing(tracer =>
            {
                tracer.AddNpgsql();
            });

        return builder;
    }
}
