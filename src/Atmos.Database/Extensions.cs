using Atmos.Database.Services;
using Atmos.Domain.Abstract;
using Microsoft.Extensions.DependencyInjection;

namespace Atmos.Database;

public static class Extensions
{
    public static IServiceCollection AddDataLayerServices(this IServiceCollection services)
    {
        services.AddScoped<IUserManager, UserManager>();

        return services;
    }
}
