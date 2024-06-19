using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.OpenTelemetry;

namespace Atmos.Services.Default;

public static class Extensions
{
    public static IHostApplicationBuilder AddAtmosDefaultServices(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.ConfigureHealthChecks();
        builder.ConfigureSerilog();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static WebApplication MapAtmosDefaultEndpoints(this WebApplication app)
    {
        var healthChecks = app.MapGroup("");

        healthChecks
            .CacheOutput("HealthChecks")
            .WithRequestTimeout("HealthChecks");

        healthChecks.MapHealthChecks("/health");
        healthChecks.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = static r => r.Tags.Contains("live")
        });

        return app;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var otelSvcName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "Unknown";

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddSource(otelSvcName);

                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(http =>
                    {
                        http.FilterHttpRequestMessage = r =>
                        {
                            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                            if (otlpEndpoint is null)
                            {
                                return true;
                            }
                            return !r.RequestUri?.AbsoluteUri.Contains(otlpEndpoint, StringComparison.Ordinal) ?? true;
                        };
                    });
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder ConfigureSerilog(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog(cfg =>
        {
            cfg
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithSpan()
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.OpenTelemetry(options =>
                {
                    options.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
                    options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "";
                    options.Protocol = OtlpProtocol.Grpc;

                    var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "Unknown";
                    var headers = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]?.Split(',') ?? [];
                    var resources = builder.Configuration["OTEL_RESOURCE_ATTRIBUTES"];

                    foreach (var header in headers)
                    {
                        var (key, value) = header.Split('=') switch
                        {
                            [var k, var v] => (k, v),
                            _ => throw new InvalidOperationException($"Invalid header format {header}")
                        };

                        options.Headers.Add(key, value);
                    }

                    if (string.IsNullOrEmpty(resources) is false)
                    {
                        var parts = resources.Split('=');

                        if (parts.Length == 2)
                        {
                            options.ResourceAttributes[parts[0]] = parts[1];
                        }
                        else
                        {
                            throw new InvalidOperationException($"Invalid resource attribute format: {resources}");
                        }
                    }

                    if (options.ResourceAttributes.ContainsKey("service.name") is false)
                    {
                        options.ResourceAttributes["service.name"] = serviceName;
                    }
                });
        });

        return builder;
    }

    private static IHostApplicationBuilder ConfigureHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRequestTimeouts(
            configure: static timeouts =>
                timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));

        builder.Services.AddOutputCache(
            configureOptions: static caching =>
                caching.AddPolicy("HealthChecks",
                    build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging =>
            {
                logging.AddOtlpExporter();
            });
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
            {
                metrics.AddOtlpExporter();
            });
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
            {
                tracing.AddOtlpExporter();
            });
        }

        return builder;
    }
}
