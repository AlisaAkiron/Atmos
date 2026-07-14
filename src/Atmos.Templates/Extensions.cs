using Microsoft.Extensions.DependencyInjection;

namespace Atmos.Templates;

public static class Extensions
{
    public static IServiceCollection AddEmailRenderer(this IServiceCollection services)
    {
        services.AddLocalization();

        services.AddTransient<TemplateRenderer>();

        return services;
    }
}
