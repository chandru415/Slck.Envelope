# ?? Slck.Envelope - Complete Developer Guide

## Overview

Three NuGet packages that make observability **ridiculously easy** - just add one line and get automatic OTEL + Serilog!

| Package | Purpose | Use When |
|---------|---------|----------|
| **`Slck.Envelope`** | CQRS with auto observability | Building CQRS/queries/commands |
| **`Slck.Envelope.MediatR`** | MediatR + auto observability | Using MediatR for CQRS |
| **`Slck.Envelope.Decorators`** | Wrap any class/method | Adding observability to existing code |

**All packages read configuration from `appsettings.json` automatically!**

---

## ?? Two Development Approaches

### Auto Approach (Recommended - Less Code!)

```csharp
// ? ONLY inject IHttpContextAccessor + YOUR dependencies
public class GetTicketQuery(
    IHttpContextAccessor httpContextAccessor,
    List<Ticket> tickets)
    : AutoObservableQuery<Ticket>(httpContextAccessor)
{
    // Logger, ActivitySource, Options available automatically!
}
```

**50-60% less boilerplate code!** [Read more ?](AUTO_DEPENDENCY_INJECTION.md)

### Standard Approach (Full Control)

```csharp
// Explicit dependency injection
public class GetTicketQuery(
    ILogger<GetTicketQuery> logger,
    ActivitySource activitySource,
    SlckEnvelopeObservabilityOptions? options,
    List<Ticket> tickets)
    : ObservableQuery<Ticket>(logger, activitySource, options)
{
    // Full control for testing and complex scenarios
}
```

**Both approaches give you automatic OTEL + Serilog!**

---

## ?? Configuration (appsettings.json)

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "MyAPI",
      "ActivitySourceVersion": "1.0.0",
      "EnableSerilogEnrichment": true,
      "EnableAutoTracing": true,
      "DefaultTags": {
        "Environment": "Production",
        "Application": "TicketAPI",
        "Team": "Platform"
      }
    }
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Master switch - disable all observability |
| `ActivitySourceName` | string | `"Slck.Envelope"` | OTEL ActivitySource name |
| `ActivitySourceVersion` | string | `"1.0.0"` | OTEL ActivitySource version |
| `EnableSerilogEnrichment` | bool | `true` | Add TraceId/SpanId to logs |
| `EnableAutoTracing` | bool | `true` | Create OTEL spans automatically |
| `DefaultTags` | object | `{}` | Tags added to ALL traces |

---

## ?? Package 1: Slck.Envelope (CQRS)

### Installation

```bash
dotnet add package Slck.Envelope
```

### Setup (ONE LINE!)

```csharp
var builder = WebApplication.CreateBuilder(args);

// ? That's it! Reads appsettings.json automatically
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

### Usage - Auto Approach (Recommended!)

```csharp
// 1. Create command - ONLY inject what YOU need!
public class CreateTicketCommand(
    IHttpContextAccessor httpContextAccessor,
    List<Ticket> tickets)
    : AutoObservableCommand<Ticket>(httpContextAccessor)
{
    public string Title { get; set; } = "";

    public override async Task<IResult> HandleAsync()
    {
        // ? Logger available automatically!
        // ? ActivitySource available automatically!
        // ? OTEL span created automatically!
        // ? Serilog enrichment automatic!
        Logger.LogInformation("Creating ticket");
        return Envelope.Ok(new Ticket { Title = Title });
    }
}

// 2. Register
builder.Services.AddScoped<CreateTicketCommand>();

// 3. Use
app.MapPost("/ticket", async (CreateTicketRequest req, CreateTicketCommand cmd) =>
{
    cmd.Title = req.Title;
    return await cmd.ExecuteAsync(); // ? Automatic OTEL + Serilog!
});
```

### Usage - Standard Approach (Full Control)

```csharp
// Explicit DI - better for testing and complex scenarios
public class CreateTicketCommand(
    ILogger<CreateTicketCommand> logger,
    ActivitySource activitySource,
    SlckEnvelopeObservabilityOptions? options,
    List<Ticket> tickets)
    : ObservableCommand<Ticket>(logger, activitySource, options)
{
    public string Title { get; set; } = "";

    public override async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Creating ticket");
        return Envelope.Ok(new Ticket { Title = Title });
    }
}
```

[See comparison ?](AUTO_DEPENDENCY_INJECTION.md)

---

## ?? Package 2: Slck.Envelope.MediatR

### Installation

```bash
dotnet add package MediatR
dotnet add package Slck.Envelope.MediatR
```

### Setup (ONE LINE!)

```csharp
// ? Registers MediatR + observability, reads appsettings.json automatically
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)
);
```

### Usage

```csharp
// 1. Create request
public record GetTicketQuery(string Id) : IRequest<IResult>;

// 2. Create handler
public class GetTicketHandler : ObservableRequestHandler<GetTicketQuery, IResult>
{
    public GetTicketHandler(
        ILogger<GetTicketHandler> logger,
        ActivitySource activitySource,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options) { }

    protected override async Task<IResult> HandleAsync(
        GetTicketQuery request,
        CancellationToken ct)
    {
        // ? Automatic OTEL + Serilog!
        Logger.LogInformation("Fetching ticket: {Id}", request.Id);
        return Envelope.Ok(ticket);
    }
}

// 3. Use
app.MapGet("/ticket/{id}", async (string id, IMediator mediator) =>
    await mediator.Send(new GetTicketQuery(id)) // ? Automatic observability!
);
```

---

## ?? Package 3: Slck.Envelope.Decorators

### Installation

```bash
dotnet add package Slck.Envelope.Decorators
```

### Setup

```csharp
// Same as Package 1
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

// Register your service
builder.Services.AddScoped<TicketService>();
```

### Usage - Inherit from ObservableService

```csharp
public class TicketService : ObservableService
{
    public TicketService(
        ILogger<TicketService> logger,
        ActivitySource activitySource,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options) { }

    public async Task<Ticket> CreateTicketAsync(string title)
    {
        // ? Wrap method call - automatic OTEL + Serilog!
        return await ExecuteObservableAsync(
            "CreateTicket",
            async () =>
            {
                // Your business logic
                return new Ticket { Title = title };
            },
            new Dictionary<string, object>
            {
                ["ticket.title"] = title
            });
    }

    public Ticket? GetTicket(string id)
    {
        // ? Sync version also works!
        return ExecuteObservable(
            "GetTicket",
            () => _tickets.FirstOrDefault(t => t.Id == id),
            new Dictionary<string, object> { ["ticket.id"] = id }
        );
    }
}
```

### Usage - Static Executor (for any code)

```csharp
// Wrap ANY existing code without changing it!
public class LegacyService
{
    private readonly ILogger _logger;
    private readonly ActivitySource _activitySource;
    private readonly SlckEnvelopeObservabilityOptions _options;

    public async Task<Data> FetchDataAsync()
    {
        // ? Wrap existing method
        return await ObservableExecutor.ExecuteAsync(
            _logger,
            _activitySource,
            "FetchData",
            async () =>
            {
                // Your existing code - don't change anything!
                return await _database.GetDataAsync();
            },
            _options
        );
    }
}
```

---

## ?? What You Get Automatically

### OTEL Traces

```
Trace: 8d3c4b2a1f6e5d7c
?? Span: POST /ticket (ASP.NET Core)
?  ?? Span: Command.CreateTicketCommand
?     Tags:
?       - command.type: CreateTicketCommand
?       - command.category: write
?       - Environment: Production (from appsettings)
?       - Application: TicketAPI (from appsettings)
?       - ticket.id: abc-123 (custom tag)
```

### Serilog Logs

```json
{
  "Message": "Executing command: CreateTicketCommand",
  "CommandName": "CreateTicketCommand",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4e5f6",
  "Environment": "Production",
  "Application": "TicketAPI"
}
```

---

## ?? Advanced: Toggle Features via Config

### Disable Observability Temporarily

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": false  // ? Turn off all observability
    }
  }
}
```

### Disable Only Tracing (keep logs)

```json
{
  "SlckEnvelope": {
    "Observability": {
      "EnableAutoTracing": false,  // ? No OTEL spans
      "EnableSerilogEnrichment": true  // ? Still enrich logs
    }
  }
}
```

### Environment-Specific Config

```json
// appsettings.Development.json
{
  "SlckEnvelope": {
    "Observability": {
      "DefaultTags": {
        "Environment": "Development"
      }
    }
  }
}

// appsettings.Production.json
{
  "SlckEnvelope": {
    "Observability": {
      "DefaultTags": {
        "Environment": "Production",
        "DataCenter": "US-East"
      }
    }
  }
}
```

---

## ?? Comparison

| Feature | Package 1 (CQRS) | Package 2 (MediatR) | Package 3 (Decorators) |
|---------|------------------|---------------------|------------------------|
| **Use Case** | New CQRS code | MediatR apps | Existing code |
| **Setup** | `AddSlckEnvelopeObservability()` | `AddSlckEnvelopeMediatR()` | `AddSlckEnvelopeObservability()` |
| **Pattern** | Base class or interface | MediatR handler | Base class or static |
| **Config** | ? appsettings.json | ? appsettings.json | ? appsettings.json |
| **Code Changes** | Inherit from base | Inherit from base | Wrap method calls |
| **Auto DI** | ? Available | ? Not yet | ? Not yet |

---

## ?? Summary

### Developers Need to Do:

1. **Install package** (`dotnet add package Slck.Envelope`)
2. **Add ONE line** (`builder.Services.AddSlckEnvelopeObservability(builder.Configuration)`)
3. **Inherit base class** OR **wrap method calls**
4. **Choose approach**: Auto (less code) or Standard (more control)
5. **Done!** OTEL + Serilog automatic!

### Configuration:

- ? Read from `appsettings.json` automatically
- ? Override with environment-specific files
- ? Toggle features without code changes
- ? Default tags for all traces

### No Extra Code Needed:

- ? No manual `Activity.StartActivity()`
- ? No manual `logger.BeginScope()`
- ? No manual TraceId/SpanId management
- ? No manual error tracking

**Result: Production-ready observability in under 5 minutes!** ??

### Learn More

- [Auto vs Standard Approach ?](AUTO_DEPENDENCY_INJECTION.md)
- [MediatR Integration ?](MEDIATR_INTEGRATION.md)
- [Quick Reference ?](QUICK_REFERENCE.md)
