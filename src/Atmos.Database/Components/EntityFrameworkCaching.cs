using Atmos.Database.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Atmos.Database;

public static class EntityFrameworkCaching
{
    /// <summary>
    /// Registers the PostgreSQL-backed cache. Requires <c>ConfigureNpgsql</c>, since
    /// every operation resolves an <see cref="AtmosDbContext" /> from a fresh scope.
    /// </summary>
    public static IHostApplicationBuilder ConfigureAtmosCache(this IHostApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton<AtmosCacheStore>();

        // Replace rather than TryAdd: an AddDistributedMemoryCache elsewhere in the
        // graph would otherwise silently win and split the cache per instance
        builder.Services.Replace(ServiceDescriptor.Singleton<IDistributedCache, EntityFrameworkDistributedCache>());

        builder.Services.AddHostedService<AtmosCacheMaintenanceService>();

        return builder;
    }
}
