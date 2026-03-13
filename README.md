<p align="center">
 <h2 align="center">Slck.Envelope</h2>
 <p align="center">🚀 ZERO-CONFIGURATION Observability for ASP.NET Core APIs<br/>
 Automatic Serilog + OpenTelemetry + API Response Standardization<br/>
 Just add configuration - we handle the rest!</p>
 <br/>
 <p align="center">
 <img src="https://img.shields.io/github/stars/chandru415/Slck.Envelope?style=for-the-badge" />
 <img src="https://img.shields.io/github/watchers/chandru415/Slck.Envelope?style=for-the-badge" />
  <a href="https://www.nuget.org/packages/Slck.Envelope/">
   <img src="https://img.shields.io/nuget/dt/Slck.Envelope?style=for-the-badge" />
 </a>
 </p>
</p>
<br/>



# :flashlight: Slck.Envelope

**Slck.Envelope v1.2+** is the **ONLY** .NET library that provides:
- ✅ **Automatic Serilog configuration** - no manual `UseSerilog()` calls
- ✅ **Automatic OpenTelemetry setup** - no manual `AddOpenTelemetry()` boilerplate
- ✅ **Standardized API responses** - consistent envelope across all endpoints
- ✅ **100% configuration-driven** - change behavior without recompiling

**Install ONE package. Write TWO lines of code. Provide CONFIGURATION values. That's it!** 🎉

---

## 🚀 Quick Start (2 Lines of Code!)

### 1. Install Package

```bash
dotnet add package Slck.Envelope
```

### 2. Add Configuration

**appsettings.json**:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": {
        "ServiceName": "MyAPI",
        "OtlpEndpoint": "http://localhost:4317"
      },
      "Serilog": {
        "WriteToConsole": true
      }
    }
  }
}
```

### 3. Enable in Program.cs

```csharp
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// ✅ ONE LINE - Automatic Serilog + OpenTelemetry!
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

var app = builder.Build();

// ✅ Use Serilog request logging
app.UseSlckEnvelopeSerilog();

// ✅ Use standardized API responses
app.UseSlckEnvelope();

app.Run();
```

**That's it! You now have:**
- ✅ Structured logging with Serilog (console, file, OTLP)
- ✅ Distributed tracing with OpenTelemetry
- ✅ Automatic trace/span correlation
- ✅ Runtime and process metrics
- ✅ Standardized error responses

**No manual Serilog setup. No manual OpenTelemetry configuration. Just configuration!** 🎉

---

## ✨ Features

### 🔭 Automatic Observability

**What's Automatic**:

| Traditional Approach | Slck.Envelope |
|---------------------|---------------|
| ❌ 30+ lines of Serilog setup | ✅ **Automatic** from config |
| ❌ 50+ lines of OpenTelemetry setup | ✅ **Automatic** from config |
| ❌ Manual enricher registration | ✅ **Automatic** (ThreadId, ProcessId, etc.) |
| ❌ Manual exporter configuration | ✅ **Automatic** (OTLP, Console, File) |
| ❌ Manual instrumentation | ✅ **Automatic** (ASP.NET Core, HTTP client) |

**Configuration Options**:

```json
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": {
        "ServiceName": "MyAPI",
        "Environment": "Production",
        "OtlpEndpoint": "http://alloy:4317",
        "EnableAspNetCoreInstrumentation": true,
        "EnableHttpClientInstrumentation": true,
        "EnableRuntimeMetrics": true,
        "AdditionalSources": ["MediatR", "NATS.Client"],
        "ExcludePaths": ["/health"]
      },
      "Serilog": {
        "MinimumLevel": "Information",
        "WriteToConsole": true,
        "WriteToFile": true,
        "FilePath": "logs/app-.log",
        "WriteToOpenTelemetry": true
      }
    }
  }
}
```

See [Automatic Configuration Guide](./docs/AUTOMATIC_CONFIGURATION.md) for complete reference.

---

### 📦 Standardized API Responses

- **Consistent Response Shape**: Every endpoint returns `ApiResponse<T>` with standardized properties:
  - `success`: Boolean indicating request status
  - `data`: The response payload (null on error)
  - `error`: Error details (null on success)
  - `meta`: Optional metadata (pagination, timestamps, etc.)
  - `requestId`: Correlation ID for tracing
  - `timestamp`: Request timestamp

- **Minimal API Integration**: Seamless integration using `IResult` implementations:
  - `Envelope.Ok(data)`: 200 OK responses
  - `Envelope.Created(location, data)`: 201 Created responses
  - `Envelope.NotFound(message)`: 404 Not Found responses
  - `Envelope.BadRequest(message, errors)`: 400 Bad Request with validation errors
  - `Envelope.Unauthorized(message)`: 401 Unauthorized responses
  - `Envelope.Error(message)`: 500 Internal Server Error

- **Exception Handling Middleware**: Automatic wrapping of unhandled exceptions into standardized error envelopes.

- **Pagination Support**: Built-in `PaginationMeta` for paginated list endpoints.

---

## 📊 Code Reduction

### Before (Manual Setup)

```csharp
// ❌ 100+ lines of boilerplate
builder.Host.UseSerilog((context, services, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .Enrich.WithProcessId()
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log")
        .WriteTo.OpenTelemetry(options => { /* ... */ });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MyAPI"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("MyAPI")
            .AddOtlpExporter(options => { /* ... */ });
    })
    .WithMetrics(metrics => { /* ... */ });

// + 50 more lines...
```

### After (Slck.Envelope)

```csharp
// ✅ 2 lines
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
app.UseSlckEnvelopeSerilog();
```

**98% code reduction!** 🎉

---

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package Slck.Envelope
```

**Includes everything you need**:
- Serilog with OTLP sink
- OpenTelemetry with ASP.NET Core instrumentation
- Structured logging enrichers
- Automatic trace correlation

---

## 📚 Documentation

- [🚀 Automatic Configuration Guide](./docs/AUTOMATIC_CONFIGURATION.md) - Complete zero-config setup
- [🔭 OpenTelemetry Configuration](./docs/OPENTELEMETRY_CONFIGURATION.md) - Deep dive into OTEL options
- [📝 Serilog Configuration](./docs/SERILOG_CONFIGURATION.md) - Deep dive into Serilog options
- [💡 Value Proposition](./docs/VALUE_PROPOSITION.md) - Why use Slck.Envelope?
- [🎯 MediatR Integration](./docs/MEDIATR_INTEGRATION.md) - Automatic observability for MediatR

---

## 🎯 Use Cases

### Development

```json
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": { "ServiceName": "MyAPI", "EnableConsoleExporter": true },
      "Serilog": { "WriteToConsole": true, "MinimumLevel": "Debug" }
    }
  }
}
```

**Result**: Console logs + console traces for debugging.

### Production

```json
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": {
        "ServiceName": "MyAPI",
        "Environment": "Production",
        "OtlpEndpoint": "http://alloy:4317"
      },
      "Serilog": {
        "WriteToFile": true,
        "FilePath": "logs/app-.log",
        "WriteToOpenTelemetry": true
      }
    }
  }
}
```

**Result**: Logs + traces → Grafana/Jaeger, local file backup.

### Docker/Kubernetes

```bash
SlckEnvelope__Observability__OpenTelemetry__OtlpEndpoint=http://alloy:4317
SlckEnvelope__Observability__OpenTelemetry__Environment=Production
```

**Result**: Environment variable overrides, no code changes!

---

## 🌟 Why Slck.Envelope?

| Feature | Slck.Envelope | Manual Setup |
|---------|---------------|--------------|
| Lines of code | **2 lines** | ~100 lines |
| Learning curve | **Low** (config only) | High (Serilog + OTEL APIs) |
| Environment-specific | **Yes** (appsettings.{env}.json) | Manual per environment |
| Docker/K8s friendly | **Yes** (env vars) | Requires code changes |
| Includes Serilog | **Yes** | Install separately |
| Includes OpenTelemetry | **Yes** | Install separately |
| Automatic enrichment | **Yes** | Manual setup |
| Standardized responses | **Yes** | Build yourself |

---

## 🙏 Credits

Built with ❤️ by **Chandrasekhar A**

Powered by:
- [Serilog](https://serilog.net/)
- [OpenTelemetry](https://opentelemetry.io/)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)

---

## 📄 License

MIT License - see [LICENSE](./LICENSE) for details.

---

## 🎉 Get Started Now!

```bash
# 1. Install
dotnet add package Slck.Envelope

# 2. Configure (appsettings.json)
{
  "SlckEnvelope": {
    "Observability": {
      "OpenTelemetry": { "ServiceName": "MyAPI", "OtlpEndpoint": "http://localhost:4317" },
      "Serilog": { "WriteToConsole": true }
    }
  }
}

# 3. Enable (Program.cs)
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

# That's it! 🚀
```

**Questions? Check [docs/AUTOMATIC_CONFIGURATION.md](./docs/AUTOMATIC_CONFIGURATION.md)!**
