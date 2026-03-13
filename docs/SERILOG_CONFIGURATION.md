# ?? Serilog Configuration for Slck.Envelope

## Overview

**Slck.Envelope** provides automatic Serilog enrichment (TraceId, SpanId, scopes), but you need to configure Serilog itself.

This guide shows how to set up Serilog with OpenTelemetry integration.

---

## ?? Required Packages

```bash
# Core Serilog
dotnet add package Serilog.AspNetCore

# OpenTelemetry sink (send logs to OTEL collector)
dotnet add package Serilog.Sinks.OpenTelemetry

# Enrichers
dotnet add package Serilog.Enrichers.CorrelationId
dotnet add package Serilog.Enrichers.Thread
dotnet add package Serilog.Enrichers.Process
dotnet add package Serilog.Enrichers.Environment

# Formatter
dotnet add package Serilog.Formatting.Compact
```

---

## ?? Configuration (appsettings.json)

### Option 1: Development (Console + File)

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Error"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithThreadId",
      "WithProcessId"
    ],
    "Properties": {
      "Application": "TicketAPI",
      "Environment": "Development"
    }
  },
  
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "TicketAPI",
      "EnableSerilogEnrichment": true,
      "EnableAutoTracing": true,
      "DefaultTags": {
        "Environment": "Development",
        "Version": "1.0.0"
      }
    }
  }
}
```

### Option 2: Production (OpenTelemetry + Console)

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.OpenTelemetry",
      "Serilog.Enrichers.CorrelationId",
      "Serilog.Enrichers.Thread",
      "Serilog.Enrichers.Process",
      "Serilog.Enrichers.Environment"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Error",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "OpenTelemetry",
        "Args": {
          "endpoint": "http://otel-collector:4317",
          "protocol": "Grpc",
          "resourceAttributes": {
            "service.name": "TicketAPI",
            "service.version": "1.0.0",
            "deployment.environment": "Production"
          }
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithCorrelationId",
      "WithThreadId",
      "WithProcessId",
      "WithProcessName",
      "WithEnvironmentUserName",
      "WithMachineName"
    ],
    "Properties": {
      "Application": "TicketAPI",
      "Environment": "Production"
    }
  },
  
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "TicketAPI",
      "EnableSerilogEnrichment": true,
      "EnableAutoTracing": true,
      "DefaultTags": {
        "Environment": "Production",
        "Version": "1.0.0",
        "Region": "US-East"
      }
    }
  }
}
```

### Option 3: Full Stack (Seq + OpenTelemetry)

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.Seq",
      "Serilog.Sinks.OpenTelemetry",
      "Serilog.Enrichers.CorrelationId"
    ],
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq:5341",
          "apiKey": "YOUR_SEQ_API_KEY"
        }
      },
      {
        "Name": "OpenTelemetry",
        "Args": {
          "endpoint": "http://otel-collector:4317",
          "protocol": "Grpc",
          "resourceAttributes": {
            "service.name": "TicketAPI",
            "service.version": "1.0.0"
          }
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithCorrelationId"
    ]
  }
}
```

---

## ?? Program.cs Setup

### Standard Serilog Setup

```csharp
using Serilog;
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// ? STEP 1: Configure Serilog from appsettings.json
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// ? STEP 2: Register Slck.Envelope Observability
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

// Register your handlers
builder.Services.AddScoped<GetTicketQuery>();

var app = builder.Build();

// ? STEP 3: Use Serilog request logging
app.UseSerilogRequestLogging();

// ? STEP 4: Use Slck.Envelope middleware
app.UseSlckEnvelope();

// Your endpoints
app.MapGet("/ticket/{id}", async (string id, GetTicketQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync();
});

app.Run();
```

---

## ?? What Each Component Does

### Slck.Envelope (Your Library)

```csharp
// Automatic enrichment INSIDE handlers
public class GetTicketQuery : ObservableQuery<Ticket>
{
    public override async Task<IResult> HandleAsync()
    {
        // ? Logger.BeginScope() called automatically with:
        // - TraceId
        // - SpanId
        // - QueryName
        // - QueryType
        
        Logger.LogInformation("Fetching ticket");  // ? Gets enriched automatically!
        
        return Envelope.Ok(ticket);
    }
}
```

### Serilog Configuration (You Set Up)

```json
{
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },           // ? WHERE logs go
      { "Name": "Seq" },               // ? WHERE logs go
      { "Name": "OpenTelemetry" }      // ? WHERE logs go
    ],
    "Enrich": [
      "WithThreadId",                  // ? WHAT to add to logs
      "WithProcessId"                  // ? WHAT to add to logs
    ]
  }
}
```

**Together**:
- ? Serilog: Configures sinks, enrichers, formatting
- ? Slck.Envelope: Adds automatic TraceId/SpanId/scopes per request

---

## ?? Log Output Examples

### Development (Console)

```
[14:32:15 INF] Executing query: GetTicketQuery {QueryName="GetTicketQuery", QueryType="Query", TraceId="8d3c4b2a1f6e5d7c", SpanId="a1b2c3d4e5f6"}
[14:32:15 INF] Fetching ticket {TicketId="123", TraceId="8d3c4b2a1f6e5d7c"}
[14:32:15 INF] Query GetTicketQuery completed successfully {TraceId="8d3c4b2a1f6e5d7c"}
```

### Production (JSON)

```json
{
  "@t": "2024-01-31T14:32:15.123Z",
  "@l": "Information",
  "@m": "Executing query: GetTicketQuery",
  "QueryName": "GetTicketQuery",
  "QueryType": "Query",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4e5f6",
  "Application": "TicketAPI",
  "Environment": "Production",
  "ThreadId": 12,
  "ProcessId": 4567
}
```

---

## ?? Integration with OpenTelemetry

### Complete Observability Stack

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ? 1. Serilog (structured logging)
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// ? 2. OpenTelemetry (distributed tracing)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("TicketAPI", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource("TicketAPI")  // ? Match ActivitySourceName in config
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
        }));

// ? 3. Slck.Envelope (automatic OTEL + Serilog integration)
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSlckEnvelope();

app.Run();
```

---

## ?? Environment-Specific Configuration

### appsettings.Development.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/dev-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  },
  "SlckEnvelope": {
    "Observability": {
      "DefaultTags": {
        "Environment": "Development"
      }
    }
  }
}
```

### appsettings.Production.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "OpenTelemetry",
        "Args": {
          "endpoint": "http://otel-collector:4317",
          "protocol": "Grpc"
        }
      }
    ]
  },
  "SlckEnvelope": {
    "Observability": {
      "DefaultTags": {
        "Environment": "Production"
      }
    }
  }
}
```

---

## ? Validation

### Test Serilog Setup

```csharp
app.MapGet("/test-logging", (ILogger<Program> logger) =>
{
    logger.LogInformation("Test log entry");
    logger.LogWarning("Test warning");
    logger.LogError("Test error");
    return Results.Ok("Check your logs!");
});
```

### Test Observability Integration

```csharp
public class TestQuery : ObservableQuery<string>
{
    public override async Task<IResult> HandleAsync()
    {
        // This log will have TraceId, SpanId, QueryName automatically!
        Logger.LogInformation("Testing observability integration");
        return Envelope.Ok("Success");
    }
}
```

---

## ?? Summary

| Component | Responsibility | Configuration |
|-----------|----------------|---------------|
| **Serilog** | Log sinks, enrichers, formatting | `appsettings.json` "Serilog" section |
| **OpenTelemetry** | Trace exporters, samplers | `Program.cs` + optional config |
| **Slck.Envelope** | Automatic TraceId/SpanId enrichment | `appsettings.json` "SlckEnvelope" section |

**Setup order**:
1. ? Configure Serilog (sinks, enrichers) ? **Your responsibility**
2. ? Configure OpenTelemetry (exporters) ? **Your responsibility**
3. ? Register Slck.Envelope observability ? **One line!**
4. ? Get automatic enrichment ? **Free!**

---

## ?? Additional Resources

- [Serilog Configuration](https://github.com/serilog/serilog/wiki/Configuration-Basics)
- [Serilog OpenTelemetry Sink](https://github.com/serilog/serilog-sinks-opentelemetry)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Seq (log aggregation)](https://datalust.co/seq)

---

**Your Slck.Envelope library handles automatic enrichment. You handle the Serilog configuration!** ??
