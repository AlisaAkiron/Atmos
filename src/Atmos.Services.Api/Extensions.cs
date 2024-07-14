using System.Reflection;
using Asp.Versioning;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Components;
using Atmos.Services.Api.Models;
using Atmos.Services.Api.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        builder.ConfigureIdentity();

        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new HeaderApiVersionReader("X-Atmos-Api-Version");
                options.UnsupportedApiVersionStatusCode = StatusCodes.Status400BadRequest;
            })
            .EnableApiVersionBinding();

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
            var svcName = builder.Configuration.GetOtelServiceName();
            options.SwaggerDoc(svcName, new OpenApiInfo { Version = "v1" });

            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "Atmos.*.xml");
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile);
            }

            options.AddOperationFilterInstance(new ApiVersionHeaderFilter());
            options.AddDocumentFilterInstance(new DefaultApiFilter());
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
            options.PreSerializeFilters.Add((oad, req) =>
            {
                oad.Servers = [new OpenApiServer { Url = $"{req.Scheme}://{req.Host.Value}" }];
            });
        });
        app.MapScalarApiReference();

        return app;
    }

    public static WebApplication MapAtmosApiEndpoints(this WebApplication app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async ctx =>
            {
                var exception = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
                var exceptionName = exception?.GetType().Name ?? "Unknown";
                var msg = exception?.Message ?? "Unknown exception issue";
                var resp = new ErrorResponse($"{exceptionName}: {msg}");
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await ctx.Response.WriteAsJsonAsync(resp);
            });
        });
        app.UseStatusCodePages(async ctx =>
        {
            var code = ctx.HttpContext.Response.StatusCode;
            var msg = new ErrorResponse($"Failed: Get status code {code}");
            await ctx.HttpContext.Response.WriteAsJsonAsync(msg);
        });

        app.UseCors();

        // app.UseAuthentication();
        // app.UseAuthorization();

        var api = app.NewVersionedApi();

        if (app.Environment.IsProduction() is false)
        {
            var svcName = app.Configuration.GetOtelServiceName();
            api.MapGet("/", () => TypedResults.Redirect($"/scalar/{svcName}"))
                .HasApiVersion(1)
                .ExcludeFromDescription();
        }

        var mappers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetInterface(nameof(IEndpointMapper)) is not null)
            .Select(x => x.GetMethod(nameof(IEndpointMapper.MapEndpoints), BindingFlags.Public | BindingFlags.Static));

        var apiGroup = api.MapGroup("/api");

        foreach (var mapper in mappers)
        {
            mapper?.Invoke(null, [apiGroup]);
        }

        return app;
    }

    private static string GetOtelServiceName(this IConfiguration configuration)
    {
        return configuration["OTEL_SERVICE_NAME"] ?? "Unknown";
    }
}
