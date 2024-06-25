using System.Reflection;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace Atmos.Services.Api;

public static class Extensions
{
    public static IHostApplicationBuilder AddAtmosApiServices(this IHostApplicationBuilder builder)
    {
        builder.ConfigureNpgsql();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("api-content", new OpenApiInfo
            {
                Version = "v1"
            });
        });

        return builder;
    }

    public static WebApplication MapAtmosOpenApiEndpoints(this WebApplication app)
    {
        if (app.Environment.IsProduction())
        {
            return app;
        }

        app.UseSwagger(options =>
        {
            options.RouteTemplate = "/openapi/{documentName}.json";
        });
        app.MapScalarApiReference();

        return app;
    }

    public static WebApplication MapAtmosApiEndpoints(this WebApplication app)
    {
        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsProduction() is false)
        {
            app.MapGet("/", () => TypedResults.Redirect("/scalar/api-content"))
                .ExcludeFromDescription();
        }

        var mappers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetInterface(nameof(IEndpointMapper)) is not null)
            .Select(x => x.GetMethod(nameof(IEndpointMapper.MapEndpoints), BindingFlags.Public | BindingFlags.Static));

        foreach (var mapper in mappers)
        {
            mapper?.Invoke(null, [app]);
        }

        return app;
    }
}
