using System.Threading.RateLimiting;
using Asp.Versioning;
using Atmos.Database;
using Atmos.Domain;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Components;
using Atmos.Services.Api.Models;
using Atmos.Services.Api.OpenApi;
using Atmos.Services.Api.Options;
using Atmos.Services.Api.Services;
using Atmos.Templates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Atmos.Services.Api;

public static class Extensions
{
    public static IHostApplicationBuilder AddAtmosApiServices(this IHostApplicationBuilder builder)
    {
        builder.ConfigureNpgsql();
        builder.ConfigureIdentity();

        var svcName = builder.Configuration.GetOtelServiceName();

        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader("X-Atmos-Api-Version");
            options.UnsupportedApiVersionStatusCode = StatusCodes.Status400BadRequest;
        });

        builder.AddAtmosCors();
        builder.AddAtmosRateLimiting();

        builder.Services.AddOpenApi(svcName, options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer<DefaultApiTransformer>();
            options.AddOperationTransformer<ApiVersionHeaderTransformer>();
        });

        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

        builder.Services.AddDomainLayerService();
        builder.Services.AddDataLayerServices();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddScoped<IUserAccountService, UserAccountService>();
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        builder.Services.AddEmailRenderer();

        return builder;
    }

    public static WebApplication MapAtmosApiEndpoints(this WebApplication app, Action<IEndpointRouteBuilder> configureEndpoints)
    {
        var isProduction = app.Environment.IsProduction();

        app.UseExceptionHandler(builder =>
        {
            builder.Run(async ctx =>
            {
                var message = "An unexpected error occurred";

                // Exception details routinely contain internals; only expose them outside production
                if (isProduction is false)
                {
                    var exception = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
                    if (exception is not null)
                    {
                        message = $"{exception.GetType().Name}: {exception.Message}";
                    }
                }

                var resp = new ErrorResponse(message);
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
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        var api = app.NewVersionedApi();

        if (isProduction is false)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();

            var svcName = app.Configuration.GetOtelServiceName();
            api.MapGet("/", () => TypedResults.Redirect($"/scalar/{svcName}"))
                .HasApiVersion(1)
                .ExcludeFromDescription();
        }

        var apiGroup = api.MapGroup("/api");

        configureEndpoints(apiGroup);

        return app;
    }

    public static IEndpointRouteBuilder MapEndpoints<TMapper>(this IEndpointRouteBuilder endpoints)
        where TMapper : IEndpointMapper
    {
        TMapper.MapEndpoints(endpoints);
        return endpoints;
    }

    private static IHostApplicationBuilder AddAtmosCors(this IHostApplicationBuilder builder)
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var isProduction = builder.Environment.IsProduction();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else if (isProduction is false)
                {
                    // Development convenience only; production requires explicit origins
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin();
                }
            });
        });

        return builder;
    }

    private static IHostApplicationBuilder AddAtmosRateLimiting(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(AtmosAuthenticationDefaults.RateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return builder;
    }

    private static string GetOtelServiceName(this IConfiguration configuration)
    {
        return configuration["OTEL_SERVICE_NAME"] ?? "Unknown";
    }
}
