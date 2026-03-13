# Slck.Envelope Observability - CQRS Pattern

## Overview

The **Slck.Envelope Observability** feature provides automatic **OpenTelemetry (OTEL) tracing** and **Serilog enrichment** for your CQRS commands and queries by simply inheriting from base classes.

## ? Key Features

- **Zero Boilerplate**: Just inherit from `ObservableCommand<T>` or `ObservableQuery<T>`
- **Automatic OTEL Tracing**: Every command/query execution creates a trace span
- **Automatic Serilog Enrichment**: Logs include TraceId, SpanId, CommandName, QueryName
- **Error Handling**: Exceptions are automatically logged and traced
- **Type-Safe**: Strongly-typed commands and queries
- **Dependency Injection**: ILogger and ActivitySource are automatically injected

## ?? Quick Start

### 1. Register Observability Services

```csharp
builder.Services.AddSlckEnvelopeObservability(options =>
{
    options.ActivitySourceName = "MyAPI";
    options.ActivitySourceVersion = "1.0.0";
});
```

### 2. Create a Command (Write Operation)

```csharp
using Slck.Envelope.Observability;

public class CreateTicketCommand : ObservableCommand<Ticket>
{
    private readonly ITicketRepository _repository;

    // ILogger and ActivitySource are auto-injected
    public CreateTicketCommand(
        ILogger<CreateTicketCommand> logger, 
        ActivitySource activitySource,
        ITicketRepository repository) 
        : base(logger, activitySource)
    {
        _repository = repository;
    }

    public string Title { get; set; } = string.Empty;

    // ? Implement your business logic - OTEL & Serilog automatic!
    protected override async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Creating ticket: {Title}", Title);

        var ticket = await _repository.CreateAsync(new Ticket { Title = Title });

        return Envelope.Created($"/ticket/{ticket.Id}", ticket);
    }
}
```

### 3. Create a Query (Read Operation)

```csharp
public class GetTicketByIdQuery : ObservableQuery<Ticket>
{
    private readonly ITicketRepository _repository;

    public GetTicketByIdQuery(
        ILogger<GetTicketByIdQuery> logger, 
        ActivitySource activitySource,
        ITicketRepository repository) 
        : base(logger, activitySource)
    {
        _repository = repository;
    }

    public string TicketId { get; set; } = string.Empty;

    // ? Implement your business logic - OTEL & Serilog automatic!
    protected override async Task<IResult> HandleAsync()
    {
        var ticket = await _repository.GetByIdAsync(TicketId);

        if (ticket is null)
            return Envelope.NotFound($"Ticket '{TicketId}' not found");

        return Envelope.Ok(ticket);
    }
}
```

### 4. Register Handlers

```csharp
builder.Services.AddScoped<CreateTicketCommand>();
builder.Services.AddScoped<GetTicketByIdQuery>();
```

### 5. Use in Endpoints

```csharp
// Query endpoint
app.MapGet("/ticket/{id}", async (string id, GetTicketByIdQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync(); // ? Auto-traced and logged!
});

// Command endpoint
app.MapPost("/ticket", async (CreateTicketRequest request, CreateTicketCommand command) =>
{
    command.Title = request.Title;
    return await command.ExecuteAsync(); // ? Auto-traced and logged!
});
```

## ?? What You Get Automatically

### OTEL Trace Spans
Each command/query execution creates a span with:
- **Name**: `Command.CreateTicketCommand` or `Query.GetTicketByIdQuery`
- **Tags**:
  - `command.type` / `query.type`
  - `command.category` / `query.category` (write/read)
  - Custom tags you add via `ActivitySource.StartActivity(...)`

### Serilog Enrichment
All logs within the command/query scope include:
- `CommandName` or `QueryName`
- `CommandType` or `QueryType`
- `TraceId` (from OTEL)
- `SpanId` (from OTEL)

### Error Handling
When an exception occurs:
- Logged with full details
- OTEL span marked as error
- Tags: `error=true`, `error.type`, `error.message`

## ?? Advanced Usage

### Adding Custom Tags to OTEL Spans

```csharp
protected override async Task<IResult> HandleAsync()
{
    // Start a child span for database operation
    using var dbActivity = ActivitySource.StartActivity("Database.Query");
    dbActivity?.SetTag("ticket.id", TicketId);
    dbActivity?.SetTag("cache.hit", false);

    var ticket = await _repository.GetByIdAsync(TicketId);
    
    return Envelope.Ok(ticket);
}
```

### Using Logger for Structured Logging

```csharp
protected override async Task<IResult> HandleAsync()
{
    // Logger is available as protected property
    Logger.LogInformation("Processing ticket {TicketId} for user {UserId}", 
        TicketId, 
        UserId);

    // All logs automatically include TraceId and SpanId
}
```

### Accessing ActivitySource

```csharp
protected override async Task<IResult> HandleAsync()
{
    // ActivitySource is available as protected property
    using var activity = ActivitySource.StartActivity("CustomOperation");
    activity?.AddEvent(new ActivityEvent("ProcessingStarted"));
    
    // Your logic
}
```

## ?? Integration with OpenTelemetry

To export traces to Jaeger, Zipkin, or other backends:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Exporter.Jaeger
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .AddSource("TicketAPI") // Match your ActivitySourceName
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.AgentHost = "localhost";
                options.AgentPort = 6831;
            });
    });
```

## ?? Integration with Serilog

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Sinks.Console
```

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TicketAPI")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));
```

## ?? Log Output Example

```
[14:32:15 INF] Executing command: CreateTicketCommand {"CommandName":"CreateTicketCommand","TraceId":"8d3c4b2a1f6e5d7c","SpanId":"a1b2c3d4e5f6"}
[14:32:15 INF] Creating ticket: New Feature Request {"CommandName":"CreateTicketCommand","TraceId":"8d3c4b2a1f6e5d7c"}
[14:32:15 INF] Command CreateTicketCommand completed successfully {"CommandName":"CreateTicketCommand","TraceId":"8d3c4b2a1f6e5d7c"}
```

## ?? Benefits

1. **Consistency**: All commands/queries follow the same pattern
2. **Maintainability**: Observability logic is centralized
3. **Debugging**: Easy to trace request flows across services
4. **Performance**: Identify slow commands/queries via distributed tracing
5. **Production Ready**: Full error tracking and logging out of the box

## ?? Migration from Non-Observable Code

**Before:**
```csharp
app.MapGet("/ticket/{id}", async (string id, ITicketRepository repo) =>
{
    var ticket = await repo.GetByIdAsync(id);
    return ticket is null 
        ? Envelope.NotFound($"Ticket '{id}' not found")
        : Envelope.Ok(ticket);
});
```

**After:**
```csharp
// 1. Create query class
public class GetTicketByIdQuery : ObservableQuery<Ticket>
{
    public GetTicketByIdQuery(ILogger<GetTicketByIdQuery> logger, ActivitySource activitySource, ITicketRepository repo) 
        : base(logger, activitySource) { _repo = repo; }
    
    public string Id { get; set; } = string.Empty;
    
    protected override async Task<IResult> HandleAsync()
    {
        var ticket = await _repo.GetByIdAsync(Id);
        return ticket is null 
            ? Envelope.NotFound($"Ticket '{Id}' not found")
            : Envelope.Ok(ticket);
    }
}

// 2. Register
builder.Services.AddScoped<GetTicketByIdQuery>();

// 3. Use
app.MapGet("/ticket/{id}", async (string id, GetTicketByIdQuery query) =>
{
    query.Id = id;
    return await query.ExecuteAsync(); // ? Now auto-traced and logged!
});
```

## ?? API Reference

### `ObservableCommand<TResult>`
Base class for write operations (commands).

**Protected Properties:**
- `ILogger Logger` - For logging
- `ActivitySource ActivitySource` - For OTEL tracing

**Abstract Method:**
- `Task<IResult> HandleAsync()` - Implement your command logic

### `ObservableQuery<TResult>`
Base class for read operations (queries).

**Protected Properties:**
- `ILogger Logger` - For logging
- `ActivitySource ActivitySource` - For OTEL tracing

**Abstract Method:**
- `Task<IResult> HandleAsync()` - Implement your query logic

### `SlckEnvelopeObservabilityOptions`
Configuration for observability features.

**Properties:**
- `string ActivitySourceName` - OTEL activity source name (default: "Slck.Envelope")
- `string ActivitySourceVersion` - Version (default: "1.0.0")
- `bool EnableSerilogEnrichment` - Enable Serilog enrichment (default: true)
- `bool EnableAutoTracing` - Enable OTEL tracing (default: true)

## ? FAQ

**Q: Can I use this without CQRS?**  
A: Yes! You can still use `Slck.Envelope` without observability. Just don't inherit from `ObservableCommand`/`ObservableQuery`.

**Q: Do I need to install OpenTelemetry packages?**  
A: No, the base functionality works without them. Install OTEL packages only if you want to export traces.

**Q: Can I use this with MediatR?**  
A: Yes! You can create MediatR handlers that inherit from `ObservableCommand`/`ObservableQuery`.

**Q: What if I don't use Serilog?**  
A: It works with any `ILogger` implementation (Microsoft.Extensions.Logging).
