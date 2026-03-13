# ?? Automatic Observability Configuration Guide

## Overview

**Slck.Envelope v1.2+** provides **ZERO-CONFIGURATION** observability! Just add configuration values - Serilog and OpenTelemetry are registered automatically.

---

## ? What's Automatic?

| Component | Traditional Approach | Slck.Envelope Approach |
|-----------|---------------------|------------------------|
| **Serilog** | `builder.Host.UseSerilog(...)` + manual sink configuration | ? **Automatic** - configured from appsettings.json |
| **OpenTelemetry** | `services.AddOpenTelemetry()` + manual exporter setup | ? **Automatic** - configured from appsettings.json |
| **ActivitySource** | Manual `new ActivitySource()` + DI registration | ? **Automatic** - registered as singleton |
| **Enrichers** | Manual `Enrich.WithThreadId()`, etc. | ? **Automatic** - enabled via config flags |
| **Exporters** | Manual OTLP/Console exporter configuration | ? **Automatic** - endpoint from config |

---

## ?? Required Packages

### Consumer App (Your Project)

```bash
# ONLY INSTALL Slck.Envelope - that's it!
dotnet add package Slck.Envelope
```

**That's all!** Serilog and OpenTelemetry packages are included as dependencies.

### Optional Packages (Advanced Scenarios)

```bash
# SQL instrumentation (if using databases)
dotnet add package OpenTelemetry.Instrumentation.SqlClient

# Entity Framework Core instrumentation
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore

# Redis instrumentation
dotnet add package OpenTelemetry.Instrumentation.StackExchangeRedis
```

---

## ?? Configuration

### Minimal Configuration (Development)

**appsettings.Development.json**:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "OpenTelemetry": {
        "ServiceName": "MyAPI",
        "EnableConsoleExporter": true
      },
      "Serilog": {
        "WriteToConsole": true
      }
    }
  }
}
```

**Program.cs**:

```csharp
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// ? ONE LINE - AUTOMATIC Serilog + OpenTelemetry!
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();

// ? Optional: Enable Serilog request logging
app.UseSlckEnvelopeSerilog();

// ? Use Slck.Envelope middleware
app.UseSlckEnvelope();

app.Run();
```

**Result**: Console logs + console OTEL traces! ??

---

### Production Configuration (Full Stack)

**appsettings.Production.json**:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "MyAPI",
      "ActivitySourceVersion": "1.0.0",
      "DefaultTags": {
        "Team": "Platform",
        "CostCenter": "Engineering"
      },
      "OpenTelemetry": {
        "Enabled": true,
        "ServiceName": "MyAPI",
        "ServiceVersion": "1.0.0",
        "Environment": "Production",
        "Region": "us-east-1",
        "OtlpEndpoint": "http://alloy:4317",
        "Protocol": "Grpc",
        "EnableConsoleExporter": false,
        "EnableAspNetCoreInstrumentation": true,
        "EnableHttpClientInstrumentation": true,
        "EnableSqlClientInstrumentation": true,
        "EnableRuntimeMetrics": true,
        "EnableProcessMetrics": true,
        "ResourceAttributes": {
          "host.name": "prod-server-01",
          "deployment.id": "v1.0.0-20240131"
        },
        "AdditionalSources": [
          "MediatR",
          "NATS.Client",
          "Redis"
        ],
        "AdditionalMeters": [
          "MediatR.*",
          "NATS.*",
          "Redis"
        ],
        "ExcludePaths": [
          "/health",
          "/metrics",
          "/swagger"
        ]
      },
      "Serilog": {
        "Enabled": true,
        "EnableRequestLogging": true,
        "EnrichWithThreadId": true,
        "EnrichWithProcessId": true,
        "EnrichWithEnvironment": true,
        "MinimumLevel": "Information",
        "MinimumLevelOverrides": {
          "Microsoft": "Warning",
          "System": "Error",
          "Microsoft.AspNetCore": "Warning"
        },
        "WriteToConsole": false,
        "WriteToFile": true,
        "FilePath": "logs/app-.log",
        "WriteToOpenTelemetry": true,
        "OpenTelemetryEndpoint": "http://alloy:4317",
        "OpenTelemetryProtocol": "Grpc"
      }
    }
  }
}
```

**Program.cs** (SAME AS DEVELOPMENT!):

```csharp
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// ? ONE LINE - reads Production config automatically!
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();
app.UseSlckEnvelopeSerilog();
app.UseSlckEnvelope();
app.Run();
```

**Result**: 
- Logs sent to Grafana Alloy (OTLP)
- Traces sent to Grafana Alloy (OTLP)
- Metrics sent to Grafana Alloy (OTLP)
- File logs in `logs/` directory
- Automatic enrichment (ThreadId, ProcessId, Environment)
- MediatR/NATS/Redis instrumentation
- Health checks excluded from tracing

---

### Environment Variables (Docker/Kubernetes)

**Override config via environment variables**:

```bash
# Docker Compose
environment:
  - SlckEnvelope__Observability__OpenTelemetry__ServiceName=MyAPI
  - SlckEnvelope__Observability__OpenTelemetry__OtlpEndpoint=http://alloy:4317
  - SlckEnvelope__Observability__OpenTelemetry__Environment=Production
  - SlckEnvelope__Observability__Serilog__WriteToOpenTelemetry=true
```

```yaml
# Kubernetes ConfigMap
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  SlckEnvelope__Observability__OpenTelemetry__ServiceName: "MyAPI"
  SlckEnvelope__Observability__OpenTelemetry__OtlpEndpoint: "http://alloy:4317"
  SlckEnvelope__Observability__OpenTelemetry__Environment: "Production"
```

**Program.cs** (NO CHANGES NEEDED!):

```csharp
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

Environment variables override appsettings.json automatically! ??

---

## ?? Configuration Options Reference

### Top-Level Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Master switch for all observability features |
| `ActivitySourceName` | `string` | `"Slck.Envelope"` | Name for OpenTelemetry ActivitySource |
| `ActivitySourceVersion` | `string` | `"1.0.0"` | Version for ActivitySource |
| `EnableSerilogEnrichment` | `bool` | `true` | Auto-enrich logs with TraceId/SpanId |
| `EnableAutoTracing` | `bool` | `true` | Auto-create spans for handlers |
| `DefaultTags` | `Dictionary<string, object>` | `{}` | Default tags for all activities |

### OpenTelemetry Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable OpenTelemetry |
| `ServiceName` | `string` | `"UnknownService"` | Service name for resource attributes |
| `ServiceVersion` | `string` | `"1.0.0"` | Service version |
| `Environment` | `string` | `"Development"` | Deployment environment |
| `Region` | `string` | `"local"` | Deployment region |
| `OtlpEndpoint` | `string?` | `null` | OTLP endpoint URL (e.g., `"http://alloy:4317"`) |
| `Protocol` | `string` | `"Grpc"` | OTLP protocol (`"Grpc"` or `"HttpProtobuf"`) |
| `EnableConsoleExporter` | `bool` | `false` | Enable console exporter for debugging |
| `EnableAspNetCoreInstrumentation` | `bool` | `true` | Instrument ASP.NET Core requests |
| `EnableHttpClientInstrumentation` | `bool` | `true` | Instrument HTTP client calls |
| `EnableSqlClientInstrumentation` | `bool` | `false` | Instrument SQL queries (requires package) |
| `EnableRuntimeMetrics` | `bool` | `true` | Collect .NET runtime metrics |
| `EnableProcessMetrics` | `bool` | `true` | Collect process metrics |
| `ResourceAttributes` | `Dictionary<string, string>` | `{}` | Additional resource attributes |
| `AdditionalSources` | `List<string>` | `[]` | Additional ActivitySources (e.g., `["MediatR"]`) |
| `AdditionalMeters` | `List<string>` | `[]` | Additional meters (e.g., `["MediatR.*"]`) |
| `ExcludePaths` | `List<string>` | `["/health"]` | Paths to exclude from tracing |

### Serilog Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Enable Serilog |
| `EnableRequestLogging` | `bool` | `true` | Enable HTTP request logging |
| `EnrichWithThreadId` | `bool` | `true` | Enrich logs with thread ID |
| `EnrichWithProcessId` | `bool` | `true` | Enrich logs with process ID |
| `EnrichWithEnvironment` | `bool` | `true` | Enrich logs with environment info |
| `MinimumLevel` | `string` | `"Information"` | Minimum log level |
| `MinimumLevelOverrides` | `Dictionary<string, string>` | `{"Microsoft": "Warning", "System": "Error"}` | Log level overrides by namespace |
| `WriteToConsole` | `bool` | `true` | Write logs to console |
| `WriteToFile` | `bool` | `false` | Write logs to file |
| `FilePath` | `string?` | `null` | File path (e.g., `"logs/app-.log"`) |
| `WriteToOpenTelemetry` | `bool` | `false` | Write logs to OTLP endpoint |
| `OpenTelemetryEndpoint` | `string?` | `null` | OTLP endpoint (uses `OpenTelemetry.OtlpEndpoint` if not set) |
| `OpenTelemetryProtocol` | `string` | `"Grpc"` | OTLP protocol |

---

## ?? Complete Examples

### Example 1: Local Development (Console Output)

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "OpenTelemetry": {
        "ServiceName": "MyAPI-Dev",
        "EnableConsoleExporter": true
      },
      "Serilog": {
        "WriteToConsole": true,
        "MinimumLevel": "Debug"
      }
    }
  }
}
```

### Example 2: Staging (File + OTLP)

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "OpenTelemetry": {
        "ServiceName": "MyAPI-Staging",
        "Environment": "Staging",
        "OtlpEndpoint": "http://staging-alloy:4317"
      },
      "Serilog": {
        "WriteToConsole": true,
        "WriteToFile": true,
        "FilePath": "logs/staging-.log",
        "WriteToOpenTelemetry": true
      }
    }
  }
}
```

### Example 3: Production (Full Observability Stack)

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "MyAPI",
      "OpenTelemetry": {
        "ServiceName": "MyAPI",
        "ServiceVersion": "2.1.0",
        "Environment": "Production",
        "Region": "us-east-1",
        "OtlpEndpoint": "http://alloy:4317",
        "EnableSqlClientInstrumentation": true,
        "AdditionalSources": ["MediatR", "NATS.Client"],
        "AdditionalMeters": ["MediatR.*", "NATS.*"],
        "ExcludePaths": ["/health", "/metrics"]
      },
      "Serilog": {
        "EnableRequestLogging": true,
        "WriteToFile": true,
        "FilePath": "logs/prod-.log",
        "WriteToOpenTelemetry": true,
        "MinimumLevel": "Information",
        "MinimumLevelOverrides": {
          "Microsoft": "Warning",
          "System": "Error"
        }
      }
    }
  }
}
```

---

## ? Validation

### 1. Check Logs

**Console**:
```
[14:32:15 INF] Executing query: GetTicketByIdQuery
TraceId: 8d3c4b2a1f6e5d7c9a1b2c3d4e5f6a7b
SpanId: a1b2c3d4e5f6a7b8
```

**File** (`logs/app-20240131.log`):
```json
{
  "@t": "2024-01-31T14:32:15.123Z",
  "@l": "Information",
  "@m": "Executing query: GetTicketByIdQuery",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4e5f6",
  "ThreadId": 12,
  "ProcessId": 4567
}
```

### 2. Check Traces (Jaeger/Grafana)

**Jaeger UI**: http://localhost:16686
- Service: `MyAPI`
- Operation: `GetTicketByIdQuery`

**Grafana Tempo**:
```
{service.name="MyAPI"} | handler.name="GetTicketByIdQuery"
```

### 3. Check Metrics (Prometheus/Grafana)

**Prometheus**: http://localhost:9090
```promql
rate(http_server_request_duration_seconds_count[5m])
```

---

## ?? Summary

| Before (Manual Setup) | After (Slck.Envelope v1.2+) |
|----------------------|----------------------------|
| ? 50+ lines of Serilog setup | ? Configuration only |
| ? 40+ lines of OTEL setup | ? Configuration only |
| ? Manual exporter configuration | ? Automatic from config |
| ? Manual enricher registration | ? Automatic from flags |
| ? Separate development/production setup | ? Environment-specific config files |
| **Total: ~100 lines** | **Total: 2 lines of code** |

---

## ?? Additional Resources

- [OpenTelemetry Configuration Deep Dive](./OPENTELEMETRY_CONFIGURATION.md)
- [Serilog Configuration Deep Dive](./SERILOG_CONFIGURATION.md)
- [Value Proposition: Why Use Slck.Envelope](./VALUE_PROPOSITION.md)
- [MediatR Integration Guide](./MEDIATR_INTEGRATION.md)
