using Atmos.Common.Abstract;
using Atmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atmos.Domain;

public static class Extensions
{
    public static IServiceCollection AddDomainLayerService(this IServiceCollection services)
    {
        // Common
        services.AddSingleton<IGuidProvider, GuidProvider>();

        return services;
    }
}
