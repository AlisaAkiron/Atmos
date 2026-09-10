using Atmos.Database;
using Atmos.Services.Api.Caching;
using Atmos.Services.Default.Caching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Atmos.Services.Api;

public static class AtmosCaching
{
    /// <summary>
    /// Wires the API's caches to PostgreSQL: <c>IDistributedCache</c> and the output
    /// cache both live in <c>atmos_cache</c>, except for health responses, which stay
    /// in process.
    /// </summary>
    internal static IHostApplicationBuilder ConfigureCaching(this IHostApplicationBuilder builder)
    {
        builder.ConfigureAtmosCache();

        builder.Services.TryAddSingleton<LocalOutputCacheStore>();
        builder.Services.TryAddSingleton<EntityFrameworkOutputCacheStore>();

        // AddOutputCache registers the in-memory store with TryAdd, so whichever of
        // the two runs first would win. Clearing first makes the outcome independent
        // of the order the service configurators happen to be called in.
        builder.Services.RemoveAll<IOutputCacheStore>();
        builder.Services.AddSingleton<IOutputCacheStore>(static provider => new RoutingOutputCacheStore(
            local: provider.GetRequiredService<LocalOutputCacheStore>(),
            shared: provider.GetRequiredService<EntityFrameworkOutputCacheStore>()));

        return builder;
    }
}
