using System.Diagnostics;

namespace Slck.Envelope.Observability;

/// <summary>
/// Configuration options for Slck.Envelope observability features.
/// All settings can be configured via appsettings.json under "SlckEnvelope:Observability" section.
/// </summary>
public class SlckEnvelopeObservabilityOptions
{
    public const string ConfigurationSectionName = "SlckEnvelope:Observability";

    /// <summary>
    /// Gets or sets whether observability features are enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the ActivitySource name for OpenTelemetry tracing.
    /// Default: "Slck.Envelope"
    /// </summary>
    public string ActivitySourceName { get; set; } = "Slck.Envelope";

    /// <summary>
    /// Gets or sets the ActivitySource version.
    /// Default: "1.0.0"
    /// </summary>
    public string ActivitySourceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets whether to enable Serilog enrichment with TraceId/SpanId.
    /// Default: true
    /// </summary>
    public bool EnableSerilogEnrichment { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic tracing for all handlers.
    /// Default: true
    /// </summary>
    public bool EnableAutoTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets default tags to add to all activities.
    /// </summary>
    public Dictionary<string, object> DefaultTags { get; set; } = new();

    // ========================================================================
    // OpenTelemetry Configuration
    // ========================================================================

    /// <summary>
    /// OpenTelemetry configuration section.
    /// </summary>
    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();

    // ========================================================================
    // Serilog Configuration
    // ========================================================================

    /// <summary>
    /// Serilog configuration section.
    /// </summary>
    public SerilogOptions Serilog { get; set; } = new();
}

/// <summary>
/// OpenTelemetry-specific configuration.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets whether OpenTelemetry is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for OpenTelemetry.
    /// Default: "UnknownService"
    /// </summary>
    public string ServiceName { get; set; } = "UnknownService";

    /// <summary>
    /// Gets or sets the service version.
    /// Default: "1.0.0"
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the deployment environment (e.g., Development, Production).
    /// Default: "Development"
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Gets or sets the deployment region.
    /// Default: "local"
    /// </summary>
    public string Region { get; set; } = "local";

    /// <summary>
    /// Gets or sets the OTLP endpoint URL.
    /// Example: "http://alloy:4317" or "http://localhost:4317"
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the OTLP protocol (Grpc or HttpProtobuf).
    /// Default: "Grpc"
    /// </summary>
    public string Protocol { get; set; } = "Grpc";

    /// <summary>
    /// Gets or sets whether to enable console exporter for debugging.
    /// Default: false
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable ASP.NET Core instrumentation.
    /// Default: true
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable HTTP client instrumentation.
    /// Default: true
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable SQL client instrumentation.
    /// Default: false (requires OpenTelemetry.Instrumentation.SqlClient)
    /// </summary>
    public bool EnableSqlClientInstrumentation { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable runtime metrics.
    /// Default: true
    /// </summary>
    public bool EnableRuntimeMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable process metrics.
    /// Default: true
    /// </summary>
    public bool EnableProcessMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets additional resource attributes.
    /// </summary>
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();

    /// <summary>
    /// Gets or sets additional activity sources to listen to.
    /// Example: ["MediatR", "NATS.Client", "Redis"]
    /// </summary>
    public List<string> AdditionalSources { get; set; } = new();

    /// <summary>
    /// Gets or sets additional meters to collect.
    /// Example: ["MediatR.*", "NATS.*", "Redis"]
    /// </summary>
    public List<string> AdditionalMeters { get; set; } = new();

    /// <summary>
    /// Gets or sets paths to exclude from tracing (e.g., health checks).
    /// Example: ["/health", "/metrics"]
    /// </summary>
    public List<string> ExcludePaths { get; set; } = new() { "/health" };
}

/// <summary>
/// Serilog-specific configuration.
/// </summary>
public class SerilogOptions
{
    /// <summary>
    /// Gets or sets whether Serilog is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use Serilog for request logging.
    /// Default: true
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with thread ID.
    /// Default: true
    /// </summary>
    public bool EnrichWithThreadId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with process ID.
    /// Default: true
    /// </summary>
    public bool EnrichWithProcessId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with environment info.
    /// Default: true
    /// </summary>
    public bool EnrichWithEnvironment { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level.
    /// Default: "Information"
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets log level overrides.
    /// Example: { "Microsoft": "Warning", "System": "Error" }
    /// </summary>
    public Dictionary<string, string> MinimumLevelOverrides { get; set; } = new()
    {
        ["Microsoft"] = "Warning",
        ["System"] = "Error"
    };

    /// <summary>
    /// Gets or sets whether to write to console.
    /// Default: true
    /// </summary>
    public bool WriteToConsole { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to write to file.
    /// Default: false
    /// </summary>
    public bool WriteToFile { get; set; } = false;

    /// <summary>
    /// Gets or sets the file path for file logging.
    /// Example: "logs/app-.log"
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets whether to write to OpenTelemetry sink.
    /// Default: false (enabled automatically if OpenTelemetry.OtlpEndpoint is set)
    /// </summary>
    public bool WriteToOpenTelemetry { get; set; } = false;

    /// <summary>
    /// Gets or sets the OpenTelemetry endpoint for Serilog sink.
    /// If not set, uses the OpenTelemetry.OtlpEndpoint value.
    /// </summary>
    public string? OpenTelemetryEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry protocol for Serilog sink.
    /// Default: "Grpc"
    /// </summary>
    public string OpenTelemetryProtocol { get; set; } = "Grpc";
}
