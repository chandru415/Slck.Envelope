using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Slck.Envelope.Observability;

/// <summary>
/// Extension methods for registering Slck.Envelope observability services.
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds Slck.Envelope observability features with AUTOMATIC Serilog and OpenTelemetry registration.
    /// Consumers only need to provide configuration - no manual Serilog or OTEL setup required!
    /// 
    /// Configuration Example (appsettings.json):
    /// <code>
    /// {
    ///   "SlckEnvelope": {
    ///     "Observability": {
    ///       "Enabled": true,
    ///       "OpenTelemetry": {
    ///         "ServiceName": "MyAPI",
    ///         "OtlpEndpoint": "http://alloy:4317"
    ///       },
    ///       "Serilog": {
    ///         "WriteToConsole": true,
    ///         "WriteToOpenTelemetry": true
    ///       }
    ///     }
    ///   }
    /// }
    /// </code>
    /// </summary>
    public static IServiceCollection AddSlckEnvelopeObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SlckEnvelopeObservabilityOptions>? configure = null)
    {
        // Register HttpContextAccessor for Auto* base classes
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Register options from configuration
        services.Configure<SlckEnvelopeObservabilityOptions>(
            configuration.GetSection(SlckEnvelopeObservabilityOptions.ConfigurationSectionName));

        // Allow programmatic configuration to override
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Build options to check if enabled
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<SlckEnvelopeObservabilityOptions>>().Value;

        if (!options.Enabled)
        {
            // Register a no-op ActivitySource if disabled
            services.TryAddSingleton(new ActivitySource("Disabled"));
            services.TryAddSingleton(options);
            return services;
        }

        // ? AUTOMATIC SERILOG REGISTRATION
        if (options.Serilog.Enabled)
        {
            ConfigureSerilog(services, options);
        }

        // ? AUTOMATIC OPENTELEMETRY REGISTRATION
        if (options.OpenTelemetry.Enabled)
        {
            ConfigureOpenTelemetry(services, options);
        }

        // Register ActivitySource as singleton
        services.TryAddSingleton(sp =>
        {
            var activitySource = new ActivitySource(options.ActivitySourceName, options.ActivitySourceVersion);
            return activitySource;
        });

        // Register options as singleton for easy access
        services.TryAddSingleton(options);

        return services;
    }

    /// <summary>
    /// Configures Serilog automatically based on options.
    /// NO EXPLICIT Serilog.UseSerilog() CALL REQUIRED BY CONSUMER!
    /// </summary>
    private static void ConfigureSerilog(IServiceCollection services, SlckEnvelopeObservabilityOptions options)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext();

        // Minimum level
        var minimumLevel = ParseLogLevel(options.Serilog.MinimumLevel);
        loggerConfig.MinimumLevel.Is(minimumLevel);

        // Minimum level overrides
        foreach (var (source, level) in options.Serilog.MinimumLevelOverrides)
        {
            var levelEnum = ParseLogLevel(level);
            loggerConfig.MinimumLevel.Override(source, levelEnum);
        }

        // Enrichers
        if (options.Serilog.EnrichWithThreadId)
        {
            loggerConfig.Enrich.WithThreadId();
        }

        if (options.Serilog.EnrichWithProcessId)
        {
            loggerConfig.Enrich.WithProcessId();
            loggerConfig.Enrich.WithProcessName();
        }

        if (options.Serilog.EnrichWithEnvironment)
        {
            loggerConfig.Enrich.WithMachineName();
        }

        // Console sink
        if (options.Serilog.WriteToConsole)
        {
            loggerConfig.WriteTo.Console();
        }

        // File sink
        if (options.Serilog.WriteToFile && !string.IsNullOrEmpty(options.Serilog.FilePath))
        {
            loggerConfig.WriteTo.File(
                options.Serilog.FilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7);
        }

        // OpenTelemetry sink
        if (options.Serilog.WriteToOpenTelemetry || !string.IsNullOrEmpty(options.Serilog.OpenTelemetryEndpoint))
        {
            var otlpEndpoint = options.Serilog.OpenTelemetryEndpoint
                ?? options.OpenTelemetry.OtlpEndpoint
                ?? "http://localhost:4317";

            var protocol = options.Serilog.OpenTelemetryProtocol.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase)
                ? Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf
                : Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;

            loggerConfig.WriteTo.OpenTelemetry(otlpOptions =>
            {
                otlpOptions.Endpoint = otlpEndpoint;
                otlpOptions.Protocol = protocol;
                otlpOptions.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = options.OpenTelemetry.ServiceName,
                    ["service.version"] = options.OpenTelemetry.ServiceVersion,
                    ["deployment.environment"] = options.OpenTelemetry.Environment
                };
            });
        }

        // Create logger
        Log.Logger = loggerConfig.CreateLogger();

        // Register Serilog with DI
        services.AddSerilog(dispose: true);
    }

    /// <summary>
    /// Configures OpenTelemetry automatically based on options.
    /// NO EXPLICIT AddOpenTelemetry() CALL REQUIRED BY CONSUMER!
    /// </summary>
    private static void ConfigureOpenTelemetry(IServiceCollection services, SlckEnvelopeObservabilityOptions options)
    {
        var otlpEndpoint = options.OpenTelemetry.OtlpEndpoint;
        var hasOtlpEndpoint = !string.IsNullOrEmpty(otlpEndpoint) && Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out _);

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: options.OpenTelemetry.ServiceName,
                    serviceVersion: options.OpenTelemetry.ServiceVersion,
                    serviceInstanceId: System.Environment.MachineName);

                var attributes = new Dictionary<string, object>
                {
                    ["environment"] = options.OpenTelemetry.Environment,
                    ["deployment.region"] = options.OpenTelemetry.Region
                };

                // Add custom resource attributes
                foreach (var (key, value) in options.OpenTelemetry.ResourceAttributes)
                {
                    attributes[key] = value;
                }

                resource.AddAttributes(attributes);
            })
            .WithTracing(tracing =>
            {
                // ASP.NET Core instrumentation
                if (options.OpenTelemetry.EnableAspNetCoreInstrumentation)
                {
                    tracing.AddAspNetCoreInstrumentation(aspNetOptions =>
                    {
                        aspNetOptions.Filter = ctx =>
                        {
                            var path = ctx.Request.Path.Value ?? string.Empty;
                            return !options.OpenTelemetry.ExcludePaths.Any(excluded =>
                                path.Contains(excluded, StringComparison.OrdinalIgnoreCase));
                        };

                        aspNetOptions.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                        };
                    });
                }

                // HTTP client instrumentation
                if (options.OpenTelemetry.EnableHttpClientInstrumentation)
                {
                    tracing.AddHttpClientInstrumentation();
                }

                // Add Slck.Envelope source
                tracing.AddSource(options.ActivitySourceName);

                // Add additional sources
                foreach (var source in options.OpenTelemetry.AdditionalSources)
                {
                    tracing.AddSource(source);
                }

                tracing.SetErrorStatusOnException();

                // OTLP Exporter
                if (hasOtlpEndpoint)
                {
                    tracing.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(otlpEndpoint!);
                        otlpOptions.Protocol = options.OpenTelemetry.Protocol.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase)
                            ? OtlpExportProtocol.HttpProtobuf
                            : OtlpExportProtocol.Grpc;
                    });
                }

                // Console exporter
                if (options.OpenTelemetry.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }

                // Fallback to console if no exporter configured
                if (!hasOtlpEndpoint && !options.OpenTelemetry.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                // ASP.NET Core metrics
                if (options.OpenTelemetry.EnableAspNetCoreInstrumentation)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                // HTTP client metrics
                if (options.OpenTelemetry.EnableHttpClientInstrumentation)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                // Runtime metrics
                if (options.OpenTelemetry.EnableRuntimeMetrics)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                // Process metrics
                if (options.OpenTelemetry.EnableProcessMetrics)
                {
                    metrics.AddProcessInstrumentation();
                }

                // Add Slck.Envelope meter
                metrics.AddMeter(options.ActivitySourceName);

                // Add additional meters
                foreach (var meter in options.OpenTelemetry.AdditionalMeters)
                {
                    metrics.AddMeter(meter);
                }

                // Default meters
                metrics.AddMeter("Microsoft.AspNetCore.Hosting");
                metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                metrics.AddMeter("System.Net.Http");

                // OTLP Exporter
                if (hasOtlpEndpoint)
                {
                    metrics.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(otlpEndpoint!);
                        otlpOptions.Protocol = options.OpenTelemetry.Protocol.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase)
                            ? OtlpExportProtocol.HttpProtobuf
                            : OtlpExportProtocol.Grpc;
                    });
                }

                // Console exporter
                if (options.OpenTelemetry.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }
            });
    }

    /// <summary>
    /// Parse log level string to Serilog LogEventLevel.
    /// </summary>
    private static LogEventLevel ParseLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}

/// <summary>
/// Extension methods for IApplicationBuilder to configure Serilog request logging.
/// </summary>
public static class ObservabilityApplicationBuilderExtensions
{
    /// <summary>
    /// Configures Serilog to use request logging.
    /// Call this AFTER AddSlckEnvelopeObservability in Program.cs.
    /// </summary>
    public static IApplicationBuilder UseSlckEnvelopeSerilog(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<SlckEnvelopeObservabilityOptions>();
        
        if (options?.Serilog.Enabled == true && options.Serilog.EnableRequestLogging)
        {
            app.UseSerilogRequestLogging();
        }

        return app;
    }
}
