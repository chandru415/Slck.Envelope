# ?? Quick Reference Card

## Setup (All Packages)

### appsettings.json
```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "MyAPI",
      "EnableSerilogEnrichment": true,
      "EnableAutoTracing": true,
      "DefaultTags": {
        "Environment": "Production"
      }
    }
  }
}
```

---

## Package 1: CQRS (Slck.Envelope)

### Install
```bash
dotnet add package Slck.Envelope
```

### Setup
```csharp
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

### Usage
```csharp
public class MyCommand : ObservableCommand<Result>
{
    public MyCommand(ILogger<MyCommand> logger, ActivitySource activitySource, SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options) { }

    public override async Task<IResult> HandleAsync()
    {
        return Envelope.Ok(result); // ? Auto OTEL + Serilog!
    }
}
```

---

## Package 2: MediatR (Slck.Envelope.MediatR)

### Install
```bash
dotnet add package Slck.Envelope.MediatR
```

### Setup
```csharp
builder.Services.AddSlckEnvelopeMediatR(builder.Configuration, cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

### Usage
```csharp
public class MyHandler : ObservableRequestHandler<MyRequest, IResult>
{
    public MyHandler(ILogger<MyHandler> logger, ActivitySource activitySource, SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options) { }

    protected override async Task<IResult> HandleAsync(MyRequest request, CancellationToken ct)
    {
        return Envelope.Ok(result); // ? Auto OTEL + Serilog!
    }
}
```

---

## Package 3: Decorators (Slck.Envelope.Decorators)

### Install
```bash
dotnet add package Slck.Envelope.Decorators
```

### Setup
```csharp
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

### Usage - Inherit Base Class
```csharp
public class MyService : ObservableService
{
    public MyService(ILogger<MyService> logger, ActivitySource activitySource, SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options) { }

    public async Task<Data> FetchAsync()
    {
        return await ExecuteObservableAsync("Fetch", async () =>
        {
            return await _db.GetDataAsync(); // ? Auto OTEL + Serilog!
        });
    }
}
```

### Usage - Static Executor
```csharp
return await ObservableExecutor.ExecuteAsync(
    logger,
    activitySource,
    "OperationName",
    async () => await DoWorkAsync(),
    options
); // ? Auto OTEL + Serilog!
```

---

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Enabled` | `true` | Turn on/off observability |
| `ActivitySourceName` | `"Slck.Envelope"` | OTEL source name |
| `EnableSerilogEnrichment` | `true` | Add TraceId to logs |
| `EnableAutoTracing` | `true` | Create OTEL spans |
| `DefaultTags` | `{}` | Tags for all traces |

---

## Toggle Features

### Disable Everything
```json
{ "SlckEnvelope": { "Observability": { "Enabled": false } } }
```

### Only Logs (No Tracing)
```json
{ "SlckEnvelope": { "Observability": { "EnableAutoTracing": false } } }
```

### Only Tracing (No Log Enrichment)
```json
{ "SlckEnvelope": { "Observability": { "EnableSerilogEnrichment": false } } }
```

---

## What You Get Automatically

? OTEL distributed traces  
? Serilog structured logs with TraceId/SpanId  
? Error tracking with trace correlation  
? Custom tags from configuration  
? Environment-specific configuration  

---

## What You DON'T Write

? `Activity.StartActivity()`  
? `logger.BeginScope()`  
? `activity.SetStatus()`  
? TraceId/SpanId management  
? Error handling in traces  

---

## Complete Example

```csharp
// appsettings.json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "TicketAPI",
      "DefaultTags": { "Environment": "Production" }
    }
  }
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
builder.Services.AddScoped<CreateTicketCommand>();

// Command
public class CreateTicketCommand : ObservableCommand<Ticket>
{
    public CreateTicketCommand(ILogger<CreateTicketCommand> logger, ActivitySource activitySource, SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options) { }

    public override async Task<IResult> HandleAsync()
    {
        return Envelope.Created("/ticket/123", ticket);
    }
}

// Endpoint
app.MapPost("/ticket", async (Request req, CreateTicketCommand cmd) =>
{
    cmd.Title = req.Title;
    return await cmd.ExecuteAsync(); // ? AUTOMATIC OTEL + SERILOG!
});
```

---

## Result

**Production-ready observability in 3 steps:**

1. ? Install package
2. ? ONE LINE setup
3. ? Inherit base class or wrap methods

**Total time: < 5 minutes** ??
