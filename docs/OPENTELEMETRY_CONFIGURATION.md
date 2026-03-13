# ?? OpenTelemetry Configuration Guide

This guide explains how to configure **OpenTelemetry (OTEL)** to work with **Slck.Envelope** for complete distributed tracing and metrics collection.

---

## ?? Table of Contents

- [Overview](#overview)
- [Required Packages](#required-packages)
- [Configuration Scenarios](#configuration-scenarios)
  - [Development (Console Exporter)](#1-development-console-exporter)
  - [Production (OTLP Exporter)](#2-production-otlp-exporter)
  - [Full Stack (Multiple Exporters)](#3-full-stack-multiple-exporters)
- [Program.cs Setup](#programcs-setup)
- [Environment-Specific Configuration](#environment-specific-configuration)
- [Integration with Slck.Envelope](#integration-with-slckenvelope)
- [Advanced Configuration](#advanced-configuration)
- [Validation](#validation)

---

## ?? Overview

### Responsibility Split

| Component | What It Does | Who Configures |
|-----------|--------------|----------------|
| **OpenTelemetry** | Trace/metric exporters, instrumentation, resource attributes | **Developer** (Program.cs) |
| **Slck.Envelope** | Automatic span creation, activity enrichment, operation tracking | **Library** (automatic) |

**Key Point**: You configure **where traces/metrics go** ? Slck.Envelope **automatically creates spans and enriches activities**!

---

## ?? Required Packages

### Core OTEL Packages

```bash
# Core OpenTelemetry
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting

# ASP.NET Core instrumentation
dotnet add package OpenTelemetry.Instrumentation.AspNetCore

# HTTP client instrumentation
dotnet add package OpenTelemetry.Instrumentation.Http

# SQL instrumentation (if using databases)
dotnet add package OpenTelemetry.Instrumentation.SqlClient

# Runtime/Process metrics
dotnet add package OpenTelemetry.Instrumentation.Runtime
dotnet add package OpenTelemetry.Instrumentation.Process
```

### Exporters

```bash
# OTLP exporter (production - sends to collectors like Grafana Alloy, Jaeger, etc.)
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol

# Console exporter (development - prints to console)
dotnet add package OpenTelemetry.Exporter.Console

# Prometheus exporter (optional - for Prometheus metrics)
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
```

### Additional Instrumentation (Optional)

```bash
# Redis instrumentation
dotnet add package OpenTelemetry.Instrumentation.StackExchangeRedis

# Entity Framework Core instrumentation
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore

# gRPC instrumentation
dotnet add package OpenTelemetry.Instrumentation.GrpcNetClient
```

---

## ?? Configuration Scenarios

### 1. Development (Console Exporter)

**Goal**: Print traces/metrics to console for debugging

#### Program.cs

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// ? STEP 1: Configure OpenTelemetry with Console Exporter
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "TicketAPI",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter(); // ? Console output for development
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter(); // ? Console output for development
    });

// ? STEP 2: Register Slck.Envelope Observability
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();

// ? STEP 3: Use Slck.Envelope middleware
app.UseSlckEnvelope();

app.Run();
```

#### Expected Console Output

```
Activity.TraceId:            8d3c4b2a1f6e5d7c9a1b2c3d4e5f6a7b
Activity.SpanId:             a1b2c3d4e5f6a7b8
Activity.TraceFlags:         Recorded
Activity.ActivitySourceName: Slck.Envelope
Activity.DisplayName:        GetTicketByIdQuery
Activity.Kind:               Internal
Activity.StartTime:          2024-01-31T14:32:15.1234567Z
Activity.Duration:           00:00:00.0234567
Activity.Tags:
    handler.type: Query
    handler.name: GetTicketByIdQuery
    handler.result: Success
```

---

### 2. Production (OTLP Exporter)

**Goal**: Send traces/metrics to centralized collector (Grafana Alloy, Jaeger, Tempo, etc.)

#### appsettings.Production.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "TicketAPI",
    "ServiceVersion": "1.0.0",
    "Endpoint": "http://alloy:4317",
    "Protocol": "Grpc",
    "Environment": "Production",
    "Region": "us-east-1"
  }
}
```

#### Program.cs

```csharp
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

// ? STEP 1: Configure OpenTelemetry with OTLP Exporter
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "TicketAPI",
            serviceVersion: builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0",
            serviceInstanceId: Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Configuration["OpenTelemetry:Environment"] ?? "Production",
            ["deployment.region"] = builder.Configuration["OpenTelemetry:Region"] ?? "unknown"
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = ctx => ctx.Request.Path != "/health"; // Exclude health checks
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress);
                };
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddSource("Slck.Envelope") // ? Slck.Envelope automatic spans
            .SetErrorStatusOnException();

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpUri))
        {
            tracing.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = otlpUri;
                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            });
        }
        else
        {
            tracing.AddConsoleExporter(); // Fallback
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter("Slck.Envelope") // ? Future metrics support
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http");

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var metricEndpoint))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = metricEndpoint;
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }
    });

// ? STEP 2: Register Slck.Envelope Observability
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();

// ? STEP 3: Use Slck.Envelope middleware
app.UseSlckEnvelope();

app.Run();
```

---

### 3. Full Stack (Multiple Exporters)

**Goal**: Send traces/metrics to multiple destinations (e.g., Grafana + Jaeger + Console)

#### appsettings.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "TicketAPI",
    "ServiceVersion": "1.0.0",
    "Endpoints": {
      "Grafana": "http://alloy:4317",
      "Jaeger": "http://jaeger:4317"
    },
    "Environment": "Production",
    "Region": "us-east-1",
    "EnableConsoleExporter": true
  }
}
```

#### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

var grafanaEndpoint = builder.Configuration["OpenTelemetry:Endpoints:Grafana"];
var jaegerEndpoint = builder.Configuration["OpenTelemetry:Endpoints:Jaeger"];
var enableConsole = builder.Configuration.GetValue<bool>("OpenTelemetry:EnableConsoleExporter");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "TicketAPI",
            serviceVersion: builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0",
            serviceInstanceId: Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Configuration["OpenTelemetry:Environment"] ?? "Production",
            ["deployment.region"] = builder.Configuration["OpenTelemetry:Region"] ?? "unknown"
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = ctx => ctx.Request.Path != "/health";
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddSource("Slck.Envelope") // ? Slck.Envelope spans
            .AddSource("MediatR")       // ? If using MediatR
            .SetErrorStatusOnException();

        // Multiple OTLP exporters
        if (!string.IsNullOrEmpty(grafanaEndpoint) && Uri.TryCreate(grafanaEndpoint, UriKind.Absolute, out var grafanaUri))
        {
            tracing.AddOtlpExporter("grafana", options =>
            {
                options.Endpoint = grafanaUri;
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        if (!string.IsNullOrEmpty(jaegerEndpoint) && Uri.TryCreate(jaegerEndpoint, UriKind.Absolute, out var jaegerUri))
        {
            tracing.AddOtlpExporter("jaeger", options =>
            {
                options.Endpoint = jaegerUri;
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        if (enableConsole)
        {
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation();

        if (!string.IsNullOrEmpty(grafanaEndpoint) && Uri.TryCreate(grafanaEndpoint, UriKind.Absolute, out var grafanaUri))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = grafanaUri;
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        if (enableConsole)
        {
            metrics.AddConsoleExporter();
        }
    });

builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();
app.UseSlckEnvelope();
app.Run();
```

---

## ?? Environment-Specific Configuration

### appsettings.Development.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "TicketAPI-Dev",
    "ServiceVersion": "1.0.0-dev",
    "EnableConsoleExporter": true,
    "Environment": "Development",
    "Region": "local"
  }
}
```

### appsettings.Production.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "TicketAPI",
    "ServiceVersion": "1.0.0",
    "Endpoint": "http://alloy:4317",
    "Protocol": "Grpc",
    "Environment": "Production",
    "Region": "us-east-1",
    "EnableConsoleExporter": false
  }
}
```

---

## ?? Integration with Slck.Envelope

### How They Work Together

```csharp
// ? YOU configure OTEL (where traces go)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddSource("Slck.Envelope") // ? Listen to Slck.Envelope spans!
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://alloy:4317");
            });
    });

// ? Slck.Envelope automatically creates spans
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

### What Slck.Envelope Does Automatically

When you execute a command/query:

```csharp
app.MapGet("/ticket/{id}", async (string id, GetTicketByIdQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync(); // ? Automatic span creation!
});
```

**Slck.Envelope automatically:**

1. **Creates Activity Span**:
   ```csharp
   Activity.Current = new Activity("GetTicketByIdQuery")
       .SetTag("handler.type", "Query")
       .SetTag("handler.name", "GetTicketByIdQuery")
       .Start();
   ```

2. **Enriches Serilog Logs**:
   ```csharp
   using (LogContext.PushProperty("TraceId", Activity.Current.TraceId))
   using (LogContext.PushProperty("SpanId", Activity.Current.SpanId))
   {
       _logger.LogInformation("Executing query: {QueryName}", "GetTicketByIdQuery");
   }
   ```

3. **Sets Status on Completion**:
   ```csharp
   Activity.Current.SetStatus(ActivityStatusCode.Ok);
   Activity.Current.Stop();
   ```

**Result**: OTEL exports the span ? Your collector (Grafana, Jaeger) displays it! ??

---

## ?? Advanced Configuration

### Custom Resource Attributes

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("TicketAPI", "1.0.0", Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
            ["deployment.region"] = builder.Configuration["REGION"] ?? "local",
            ["host.name"] = Environment.MachineName,
            ["os.type"] = Environment.OSVersion.Platform.ToString(),
            ["process.runtime.name"] = ".NET",
            ["process.runtime.version"] = Environment.Version.ToString(),
            ["team"] = "Platform",
            ["cost.center"] = "Engineering"
        }));
```

### Custom Instrumentation for MediatR

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Slck.Envelope")      // ? CQRS handlers
            .AddSource("MediatR")             // ? MediatR requests
            .AddSource("YourApp.Custom");    // ? Your custom spans
    });
```

### Database Instrumentation

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true; // Include SQL queries
                options.RecordException = true;
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            });
    });
```

### Redis Instrumentation

```csharp
// First, configure Redis connection
var redisConnection = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// Then add instrumentation
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddRedisInstrumentation(redisConnection);
    });
```

### HTTP Client Filtering

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation(options =>
            {
                // Don't trace health check endpoints
                options.FilterHttpRequestMessage = (request) =>
                {
                    return !request.RequestUri?.AbsolutePath.Contains("/health") ?? true;
                };

                // Enrich spans with custom tags
                options.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    activity.SetTag("http.request.method", request.Method.ToString());
                };
            });
    });
```

---

## ? Validation

### 1. Console Exporter Validation

Run your application and check console output:

```bash
dotnet run
```

**Expected Output**:
```
Activity.TraceId:            8d3c4b2a1f6e5d7c9a1b2c3d4e5f6a7b
Activity.SpanId:             a1b2c3d4e5f6a7b8
Activity.DisplayName:        GetTicketByIdQuery
Activity.Tags:
    handler.type: Query
    handler.name: GetTicketByIdQuery
    handler.result: Success
```

### 2. OTLP Exporter Validation

Check your OTEL collector (Grafana, Jaeger, etc.):

**Jaeger UI**: http://localhost:16686
- Search for service: `TicketAPI`
- Filter by operation: `GetTicketByIdQuery`

**Grafana Tempo**: Query for trace ID:
```
{service.name="TicketAPI"} | handler.name="GetTicketByIdQuery"
```

### 3. Verify Span Hierarchy

```
HTTP Request (ASP.NET Core)
  ?? GetTicketByIdQuery (Slck.Envelope) ? Automatic!
      ?? Database Query (SqlClient)
```

---

## ?? Complete Example: Production Setup

### appsettings.Production.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "TicketAPI",
    "ServiceVersion": "1.0.0",
    "Endpoint": "http://alloy:4317",
    "Protocol": "Grpc",
    "Environment": "Production",
    "Region": "us-east-1"
  },
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "TicketAPI"
    }
  },
  "Serilog": {
    "WriteTo": [
      {
        "Name": "OpenTelemetry",
        "Args": {
          "endpoint": "http://alloy:4317",
          "protocol": "Grpc"
        }
      }
    ],
    "Enrich": ["FromLogContext"]
  }
}
```

### Program.cs

```csharp
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

// ? STEP 1: Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

// ? STEP 2: Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "TicketAPI",
            serviceVersion: builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0",
            serviceInstanceId: Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Configuration["OpenTelemetry:Environment"] ?? "Production",
            ["deployment.region"] = builder.Configuration["OpenTelemetry:Region"] ?? "unknown"
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = ctx => ctx.Request.Path != "/health";
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddSource("Slck.Envelope") // ? Automatic spans!
            .SetErrorStatusOnException();

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpUri))
        {
            tracing.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = otlpUri;
                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation();

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var metricEndpoint))
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = metricEndpoint;
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }
    });

// ? STEP 3: Register Slck.Envelope Observability
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

// ? STEP 4: Register MediatR (if using)
builder.Services.AddSlckEnvelopeMediatR(builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();

// ? STEP 5: Use middleware
app.UseSerilogRequestLogging();
app.UseSlckEnvelope();

app.Run();
```

---

## ?? Summary

| You Configure | Slck.Envelope Does Automatically |
|---------------|----------------------------------|
| ? OTLP endpoint | ? Creates activity spans |
| ? Service name/version | ? Enriches with TraceId/SpanId |
| ? Resource attributes | ? Tags spans with handler info |
| ? Instrumentation libraries | ? Logs with structured context |
| ? Exporters (OTLP, Console, Jaeger) | ? Sets status on success/error |

**Result**: Complete distributed tracing with zero boilerplate in your handlers! ??

---

## ?? Additional Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Slck.Envelope Serilog Configuration](./SERILOG_CONFIGURATION.md)
- [Slck.Envelope MediatR Integration](./MEDIATR_INTEGRATION.md)
- [Value Proposition: Why Use Slck.Envelope](./VALUE_PROPOSITION.md)
