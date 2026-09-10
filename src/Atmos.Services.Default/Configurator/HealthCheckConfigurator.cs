using Atmos.Services.Default.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Atmos.Services.Default.Configurator;

internal static class HealthCheckConfigurator
{
    internal static IHostApplicationBuilder ConfigureHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRequestTimeouts(
            configure: static timeouts =>
                timeouts.AddPolicy(AtmosOutputCache.HealthChecksPolicy, TimeSpan.FromSeconds(5)));

        builder.Services.AddOutputCache(
            configureOptions: static caching =>
                caching.AddPolicy(AtmosOutputCache.HealthChecksPolicy,
                    build: static policy => policy
                        .Expire(TimeSpan.FromSeconds(10))
                        // Probes hit every instance on their own schedule and the
                        // response is cheap to rebuild, so this one stays in process
                        // instead of costing a round trip to the shared store
                        .SetCacheKeyPrefix(AtmosOutputCache.LocalKeyPrefix)));

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }
}
