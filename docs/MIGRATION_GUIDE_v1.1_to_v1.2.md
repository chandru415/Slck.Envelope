# ?? Migration Guide: v1.1 ? v1.2 (Zero-Configuration)

## Overview

**Slck.Envelope v1.2** introduces **AUTOMATIC** Serilog and OpenTelemetry registration. If you're upgrading from v1.1, you can significantly simplify your code!

---

## What Changed

| Component | v1.1 (Manual) | v1.2 (Automatic) |
|-----------|---------------|------------------|
| **Serilog** | Manual `builder.Host.UseSerilog()` | ? **Automatic** from config |
| **OpenTelemetry** | Manual `services.AddOpenTelemetry()` | ? **Automatic** from config |
| **Package Dependencies** | Consumer installs separately | ? **Included** in Slck.Envelope |

---

## Migration Steps

### Step 1: Update Package

```bash
dotnet add package Slck.Envelope --version 1.2.0
```

or

```xml
<PackageReference Include="Slck.Envelope" Version="1.2.0" />
```

---

### Step 2: Remove Manual Serilog Setup

**Before (v1.1)**:

```csharp
// ? REMOVE THIS
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .Enrich.WithProcessId()
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = "http://alloy:4317";
            options.Protocol = OtlpProtocol.Grpc;
        });
});
```

**After (v1.2)**:

```csharp
// ? Configuration only (appsettings.json)
{
  "SlckEnvelope": {
    "Observability": {
      "Serilog": {
        "WriteToConsole": true,
        "WriteToFile": true,
        "FilePath": "logs/app-.log",
        "WriteToOpenTelemetry": true,
        "OpenTelemetryEndpoint": "http://alloy:4317"
      }
    }
  }
}
```

---

### Step 3: Remove Manual OpenTelemetry Setup

**Before (v1.1)**:

```csharp
// ? REMOVE THIS
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MyAPI", "1.0.0", Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = "Production",
            ["deployment.region"] = "us-east-1"
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Slck.Envelope")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://alloy:4317");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://alloy:4317");
            });
    });
```

**After (v1.2)**:

```csharp
// ? Configuration only (appsettings.json)
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": {
        "ServiceName": "MyAPI",
        "ServiceVersion": "1.0.0",
        "Environment": "Production",
        "Region": "us-east-1",
        "OtlpEndpoint": "http://alloy:4317",
        "EnableAspNetCoreInstrumentation": true,
        "EnableHttpClientInstrumentation": true,
        "EnableRuntimeMetrics": true
      }
    }
  }
}
```

---

### Step 4: Update AddSlckEnvelopeObservability Call

**Before (v1.1)**:

```csharp
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

**After (v1.2)**:

```csharp
// ? Same call, but now it auto-configures Serilog + OTEL!
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

// ? Optional: Add Serilog request logging
app.UseSlckEnvelopeSerilog();
```

---

### Step 5: Move Configuration to appsettings.json

**Create or update** `appsettings.json`:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "MyAPI",
      "OpenTelemetry": {
        "Enabled": true,
        "ServiceName": "MyAPI",
        "ServiceVersion": "1.0.0",
        "Environment": "Development",
        "Region": "local",
        "OtlpEndpoint": "http://localhost:4317",
        "EnableConsoleExporter": true,
        "EnableAspNetCoreInstrumentation": true,
        "EnableHttpClientInstrumentation": true,
        "EnableRuntimeMetrics": true
      },
      "Serilog": {
        "Enabled": true,
        "WriteToConsole": true,
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

**Create or update** `appsettings.Production.json`:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": {
        "Environment": "Production",
        "OtlpEndpoint": "http://alloy:4317",
        "EnableConsoleExporter": false
      },
      "Serilog": {
        "WriteToConsole": false,
        "WriteToFile": true,
        "FilePath": "logs/app-.log",
        "WriteToOpenTelemetry": true
      }
    }
  }
}
```

---

### Step 6: Remove Manual Package References (Optional)

If you installed these packages manually in v1.1, you can now **remove** them:

```xml
<!-- ? REMOVE THESE (now included in Slck.Envelope) -->
<PackageReference Include="Serilog.AspNetCore" Version="..." />
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="..." />
<PackageReference Include="Serilog.Enrichers.Thread" Version="..." />
<PackageReference Include="Serilog.Enrichers.Process" Version="..." />
<PackageReference Include="OpenTelemetry" Version="..." />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="..." />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="..." />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="..." />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="..." />
```

**Slck.Envelope v1.2** includes all of these as transitive dependencies!

---

## Complete Example

### Before (v1.1) - ~100 Lines

```csharp
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// Manual Serilog setup
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .Enrich.WithProcessId()
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = "http://alloy:4317";
            options.Protocol = OtlpProtocol.Grpc;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "MyAPI",
                ["service.version"] = "1.0.0"
            };
        });
});

// Manual OpenTelemetry setup
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MyAPI", "1.0.0", Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = "Production",
            ["deployment.region"] = "us-east-1"
        }))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = ctx => ctx.Request.Path != "/health";
            })
            .AddHttpClientInstrumentation()
            .AddSource("Slck.Envelope")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://alloy:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://alloy:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    });

// Slck.Envelope setup
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSlckEnvelope();
app.Run();
```

---

### After (v1.2) - 2 Lines!

**Program.cs**:

```csharp
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// ? ONE LINE - Automatic Serilog + OpenTelemetry!
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();

// ? Use Serilog request logging
app.UseSlckEnvelopeSerilog();

// ? Use Slck.Envelope middleware
app.UseSlckEnvelope();

app.Run();
```

**appsettings.Production.json**:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "MyAPI",
      "OpenTelemetry": {
        "ServiceName": "MyAPI",
        "ServiceVersion": "1.0.0",
        "Environment": "Production",
        "Region": "us-east-1",
        "OtlpEndpoint": "http://alloy:4317",
        "EnableAspNetCoreInstrumentation": true,
        "EnableHttpClientInstrumentation": true,
        "EnableRuntimeMetrics": true,
        "ExcludePaths": ["/health"]
      },
      "Serilog": {
        "WriteToFile": true,
        "FilePath": "logs/app-.log",
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

## Configuration Mapping

Here's how your manual setup maps to configuration:

### Serilog

| Manual Code | Configuration Property |
|-------------|------------------------|
| `.WriteTo.Console()` | `"Serilog": { "WriteToConsole": true }` |
| `.WriteTo.File("logs/app-.log")` | `"Serilog": { "WriteToFile": true, "FilePath": "logs/app-.log" }` |
| `.WriteTo.OpenTelemetry(...)` | `"Serilog": { "WriteToOpenTelemetry": true, "OpenTelemetryEndpoint": "http://alloy:4317" }` |
| `.Enrich.WithThreadId()` | `"Serilog": { "EnrichWithThreadId": true }` |
| `.Enrich.WithProcessId()` | `"Serilog": { "EnrichWithProcessId": true }` |
| `.MinimumLevel.Information()` | `"Serilog": { "MinimumLevel": "Information" }` |
| `.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)` | `"Serilog": { "MinimumLevelOverrides": { "Microsoft": "Warning" } }` |

### OpenTelemetry

| Manual Code | Configuration Property |
|-------------|------------------------|
| `resource.AddService("MyAPI", "1.0.0")` | `"OpenTelemetry": { "ServiceName": "MyAPI", "ServiceVersion": "1.0.0" }` |
| `resource.AddAttributes({"environment": "Production"})` | `"OpenTelemetry": { "Environment": "Production" }` |
| `.AddAspNetCoreInstrumentation()` | `"OpenTelemetry": { "EnableAspNetCoreInstrumentation": true }` |
| `.AddHttpClientInstrumentation()` | `"OpenTelemetry": { "EnableHttpClientInstrumentation": true }` |
| `.AddRuntimeInstrumentation()` | `"OpenTelemetry": { "EnableRuntimeMetrics": true }` |
| `.AddSource("Slck.Envelope")` | Automatic! |
| `.AddOtlpExporter(new Uri("http://alloy:4317"))` | `"OpenTelemetry": { "OtlpEndpoint": "http://alloy:4317" }` |
| `.AddConsoleExporter()` | `"OpenTelemetry": { "EnableConsoleExporter": true }` |
| `options.Filter = ctx => ctx.Request.Path != "/health"` | `"OpenTelemetry": { "ExcludePaths": ["/health"] }` |

---

## Benefits of v1.2

| Benefit | Details |
|---------|---------|
| **Less Code** | 100+ lines ? 2 lines (98% reduction) |
| **Configuration-Driven** | Change behavior without recompiling |
| **Environment-Specific** | `appsettings.Development.json`, `appsettings.Production.json` |
| **Docker/K8s Friendly** | Override via environment variables |
| **Fewer Packages** | All dependencies included |
| **Easier Testing** | Mock configuration, no setup code |

---

## Troubleshooting

### My custom Serilog sinks aren't working

**v1.2** configures Serilog automatically from configuration. If you need custom sinks not supported by configuration, you can:

**Option 1**: Continue using manual `UseSerilog()` and disable automatic Serilog:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Serilog": {
        "Enabled": false  // Disable automatic Serilog
      }
    }
  }
}
```

```csharp
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .WriteTo.YourCustomSink(/* ... */);
});

builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

**Option 2**: Request feature for new sink support via GitHub Issues.

---

### My custom OpenTelemetry exporters aren't working

Similar to Serilog, disable automatic OpenTelemetry:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": {
        "Enabled": false  // Disable automatic OpenTelemetry
      }
    }
  }
}
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddYourCustomExporter());

builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

---

### Environment variables aren't working

Ensure proper naming convention:

```bash
# ? CORRECT
SlckEnvelope__Observability__OpenTelemetry__ServiceName=MyAPI

# ? INCORRECT
SlckEnvelope:Observability:OpenTelemetry:ServiceName=MyAPI
```

Use **double underscores** (`__`) instead of colons (`:`) for environment variables!

---

## FAQ

### Q: Can I still use manual setup?

**A:** Yes! Set `"Enabled": false` in configuration and use manual `UseSerilog()` and `AddOpenTelemetry()` calls.

### Q: Do I need to install Serilog/OpenTelemetry packages separately?

**A:** No! v1.2 includes all required packages as dependencies.

### Q: Can I mix automatic and manual configuration?

**A:** Partially. You can disable automatic Serilog but keep automatic OpenTelemetry (or vice versa) by setting `"Enabled": false` in the respective section.

### Q: Will this break my existing v1.1 setup?

**A:** No! If you keep your manual setup and don't add the new configuration, v1.2 behaves exactly like v1.1. Migration is opt-in.

---

## Summary

**Migration is simple**:

1. ? Update to v1.2
2. ? Remove manual Serilog/OTEL setup code
3. ? Add configuration to appsettings.json
4. ? Remove manual package references (optional)

**Result**: 98% less code, 100% configuration-driven! ??

---

For complete configuration reference, see:
- [Automatic Configuration Guide](./AUTOMATIC_CONFIGURATION.md)
- [OpenTelemetry Configuration](./OPENTELEMETRY_CONFIGURATION.md)
- [Serilog Configuration](./SERILOG_CONFIGURATION.md)
