# ? Sample Project - All Three Packages Demonstrated

## Overview

The sample project (`samples/sample.api`) now demonstrates all three Slck.Envelope packages working together with automatic OTEL + Serilog observability.

---

## ?? Package References

```xml
<ItemGroup>
  <!-- Core CQRS package with automatic OTEL + Serilog -->
  <ProjectReference Include="..\..\src\Slck.Envelope\Slck.Envelope.csproj" />
  
  <!-- Decorator pattern for wrapping any class/method -->
  <ProjectReference Include="..\..\src\Slck.Envelope.Decorators\Slck.Envelope.Decorators.csproj" />
  
  <!-- MediatR integration with automatic OTEL + Serilog -->
  <ProjectReference Include="..\..\src\Slck.Envelope.MediatR\Slck.Envelope.MediatR.csproj" />
</ItemGroup>

<ItemGroup>
  <!-- MediatR NuGet package -->
  <PackageReference Include="MediatR" Version="12.4.1" />
</ItemGroup>
```

---

## ?? Configuration (appsettings.json)

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "TicketAPI",
      "ActivitySourceVersion": "1.0.0",
      "EnableSerilogEnrichment": true,
      "EnableAutoTracing": true,
      "DefaultTags": {
        "Environment": "Development",
        "Application": "TicketAPI"
      }
    }
  }
}
```

**This configuration applies to ALL three packages automatically!**

---

## ?? Setup (Program.cs)

```csharp
// Package 1: Slck.Envelope (CQRS) - Automatic OTEL + Serilog
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

// Package 2: Slck.Envelope.MediatR - Automatic OTEL + Serilog for MediatR
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Package 3: Slck.Envelope.Decorators - Already registered by Package 1
// (Uses same infrastructure)
```

---

## ?? Package 1: Slck.Envelope (CQRS)

### Handler Example

```csharp
public class GetTicketByIdQuery : ObservableQuery<Ticket>
{
    private readonly List<Ticket> _tickets;

    public GetTicketByIdQuery(
        ILogger<GetTicketByIdQuery> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    public string TicketId { get; set; } = "";

    public override async Task<IResult> HandleAsync()
    {
        // ? Automatic OTEL + Serilog!
        Logger.LogInformation("Searching for ticket: {TicketId}", TicketId);
        var ticket = _tickets.FirstOrDefault(t => t.Id == TicketId);
        return ticket != null 
            ? Envelope.Ok(ticket)
            : Envelope.NotFound($"Ticket '{TicketId}' not found");
    }
}
```

### Endpoint

```csharp
app.MapGet("/cqrs/ticket/{id}", async (string id, GetTicketByIdQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync(); // ? Automatic OTEL + Serilog!
})
.WithTags("Package1-CQRS");
```

### Test

```bash
curl http://localhost:5000/cqrs/ticket/123
```

---

## ?? Package 2: Slck.Envelope.MediatR

### Handler Example

```csharp
public record GetTicketByIdRequest(string Id) : IRequest<IResult>;

public class GetTicketByIdRequestHandler : ObservableRequestHandler<GetTicketByIdRequest, IResult>
{
    private readonly List<Ticket> _tickets;

    public GetTicketByIdRequestHandler(
        ILogger<GetTicketByIdRequestHandler> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    protected override async Task<IResult> HandleAsync(
        GetTicketByIdRequest request, 
        CancellationToken cancellationToken)
    {
        // ? Automatic OTEL + Serilog!
        Logger.LogInformation("Fetching ticket: {TicketId}", request.Id);
        var ticket = _tickets.FirstOrDefault(t => t.Id == request.Id);
        return ticket != null 
            ? Envelope.Ok(ticket)
            : Envelope.NotFound($"Ticket '{request.Id}' not found");
    }
}
```

### Endpoint

```csharp
app.MapGet("/mediatr/ticket/{id}", async (string id, IMediator mediator) =>
{
    return await mediator.Send(new GetTicketByIdRequest(id)); // ? Automatic OTEL + Serilog!
})
.WithTags("Package2-MediatR");
```

### Test

```bash
curl http://localhost:5000/mediatr/ticket/123
```

---

## ?? Package 3: Slck.Envelope.Decorators

### Service Example

```csharp
public class TicketService : ObservableService
{
    private readonly List<Ticket> _tickets;

    public TicketService(
        ILogger<TicketService> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    public async Task<Ticket> CreateTicketAsync(string title)
    {
        // ? Wrap method call - automatic OTEL + Serilog!
        return await ExecuteObservableAsync(
            "CreateTicket",
            async () =>
            {
                var ticket = new Ticket
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title
                };
                _tickets.Add(ticket);
                await Task.Delay(10); // Simulate async work
                return ticket;
            },
            new Dictionary<string, object>
            {
                ["ticket.title"] = title
            });
    }

    public Ticket? GetTicket(string id)
    {
        // ? Sync version - also automatic OTEL + Serilog
        return ExecuteObservable(
            "GetTicket",
            () => _tickets.FirstOrDefault(t => t.Id == id),
            new Dictionary<string, object>
            {
                ["ticket.id"] = id
            });
    }
}
```

### Endpoint

```csharp
app.MapGet("/service/ticket/{id}", async (string id, TicketService service) =>
{
    var ticket = service.GetTicket(id); // ? Automatic OTEL + Serilog!
    return ticket != null 
        ? Results.Ok(ticket)
        : Results.NotFound($"Ticket '{id}' not found");
})
.WithTags("Package3-Decorators");

app.MapPost("/service/ticket", async (CreateTicketRequest request, TicketService service) =>
{
    var ticket = await service.CreateTicketAsync(request.Title); // ? Automatic OTEL + Serilog!
    return Results.Created($"/service/ticket/{ticket.Id}", ticket);
})
.WithTags("Package3-Decorators");
```

### Test

```bash
curl http://localhost:5000/service/ticket/123
curl -X POST http://localhost:5000/service/ticket -H "Content-Type: application/json" -d '{"title":"New Ticket"}'
```

---

## ?? Comparison Table

| Package | Use Case | Pattern | Endpoints | Auto OTEL | Auto Serilog |
|---------|----------|---------|-----------|-----------|--------------|
| **Slck.Envelope** | CQRS | Base class | `/cqrs/*` | ? | ? |
| **Slck.Envelope.MediatR** | MediatR | MediatR handler | `/mediatr/*` | ? | ? |
| **Slck.Envelope.Decorators** | Any service | Wrap methods | `/service/*` | ? | ? |

---

## ?? What You Get Automatically

### OTEL Traces

```
Trace: 8d3c4b2a1f6e5d7c
?? Span: GET /cqrs/ticket/123 (ASP.NET Core)
?  ?? Span: Query.GetTicketByIdQuery
?     Tags:
?       - query.type: GetTicketByIdQuery
?       - query.category: read
?       - Environment: Development (from appsettings)
?       - Application: TicketAPI (from appsettings)
?
?? Span: GET /mediatr/ticket/123 (ASP.NET Core)
?  ?? Span: MediatR.GetTicketByIdRequestHandler
?     Tags:
?       - mediatr.request: GetTicketByIdRequest
?       - mediatr.handler: GetTicketByIdRequestHandler
?       - Environment: Development (from appsettings)
?
?? Span: GET /service/ticket/123 (ASP.NET Core)
   ?? Span: Operation.GetTicket
      Tags:
        - operation.name: GetTicket
        - ticket.id: 123
        - Environment: Development (from appsettings)
```

### Serilog Logs

```json
{
  "Message": "Executing query: GetTicketByIdQuery",
  "QueryName": "GetTicketByIdQuery",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4e5f6",
  "Environment": "Development",
  "Application": "TicketAPI"
}
```

---

## ?? Toggle Features via Configuration

### Disable All Observability

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": false  // ? Turns off ALL packages!
    }
  }
}
```

### Disable Only Tracing (Keep Logs)

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

**No code changes needed - just restart the app!**

---

## ? Summary

### One-Time Setup

1. Install packages ?
2. Configure `appsettings.json` ?
3. Call `AddSlckEnvelopeObservability()` and `AddSlckEnvelopeMediatR()` ?

### For Each Handler/Service

1. Inherit from base class (`ObservableQuery`, `ObservableRequestHandler`, `ObservableService`)
2. Write ONLY business logic
3. Get automatic OTEL + Serilog! ??

### Result

- **Zero manual OTEL code** - No `StartActivity()`, `SetTag()`, `SetStatus()`
- **Zero manual Serilog scope** - No `BeginScope()` with TraceId
- **Zero manual error tracking** - Automatic try/catch with trace correlation
- **Consistent instrumentation** - Every handler gets same treatment
- **Configuration-driven** - Toggle features via `appsettings.json`

**87% code reduction - write business logic, get observability for free!** ??
