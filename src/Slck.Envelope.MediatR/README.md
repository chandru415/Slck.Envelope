# Slck.Envelope.MediatR

Extension package that provides automatic **OTEL tracing** and **Serilog logging** for **MediatR** requests.

## Why This Approach Instead of Forking MediatR?

? **Keep official MediatR** - Get updates, bug fixes, community packages  
? **Add-on pattern** - Install alongside MediatR, don't replace it  
? **MIT License compliant** - Uses MediatR as dependency, doesn't modify it  
? **Zero breaking changes** - Works with existing MediatR code  

---

## Installation

```bash
# Install official MediatR
dotnet add package MediatR

# Install Slck.Envelope.MediatR extension
dotnet add package Slck.Envelope.MediatR
```

---

## Usage

### Option 1: Automatic Observability for All Requests (Recommended)

```csharp
using Slck.Envelope.MediatR;
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// Register observability infrastructure
builder.Services.AddSlckEnvelopeObservability();

// Register MediatR with automatic OTEL + Serilog
builder.Services.AddSlckEnvelopeMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```

**Result**: Every MediatR request automatically gets:
- ? OTEL distributed tracing
- ? Serilog structured logging with TraceId/SpanId
- ? Error tracking with trace correlation

### Option 2: Per-Handler Observability

```csharp
using Slck.Envelope.MediatR;

// Your MediatR request
public record GetTicketQuery(string Id) : IRequest<IResult>;

// Handler with automatic observability
public class GetTicketHandler : ObservableRequestHandler<GetTicketQuery, IResult>
{
    private readonly ITicketRepository _repository;

    public GetTicketHandler(
        ILogger<GetTicketHandler> logger,
        ActivitySource activitySource,
        ITicketRepository repository)
        : base(logger, activitySource)
    {
        _repository = repository;
    }

    protected override async Task<IResult> HandleAsync(GetTicketQuery request, CancellationToken ct)
    {
        // Logger and ActivitySource are available
        Logger.LogInformation("Fetching ticket: {Id}", request.Id);
        
        var ticket = await _repository.GetByIdAsync(request.Id);
        
        Activity.Current?.SetTag("ticket.found", ticket != null);
        
        return ticket != null 
            ? Envelope.Ok(ticket)
            : Envelope.NotFound($"Ticket '{request.Id}' not found");
    }
}
```

---

## What You Get

### OTEL Traces

```
Trace: 8d3c4b2a1f6e5d7c
?? Span: POST /ticket
?  ?? Span: MediatR.Pipeline.GetTicketQuery
?     ?? Span: MediatR.GetTicketHandler
?        ?? Span: Database.Query (your custom span)
```

### Serilog Logs

```json
{
  "Message": "MediatR Pipeline: Processing GetTicketQuery",
  "MediatRRequest": "GetTicketQuery",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4"
}
{
  "Message": "Fetching ticket: 123",
  "HandlerName": "GetTicketHandler",
  "TraceId": "8d3c4b2a1f6e5d7c"
}
```

---

## Comparison: Fork vs Extension

| Aspect | Fork MediatR | Slck.Envelope.MediatR Extension |
|--------|--------------|----------------------------------|
| **Maintenance** | You own all updates | Use official MediatR updates |
| **Package Name** | `Slck.MediatR` | `Slck.Envelope.MediatR` + `MediatR` |
| **Breaking Changes** | Isolated from MediatR | Follow MediatR versioning |
| **Community Packages** | Won't work (different package) | ? Works (MediatR.Extensions, etc.) |
| **License** | MIT (keep notice) | MIT (dependency) |
| **Code Changes** | Modify MediatR source | Wrap with behaviors/base classes |

---

## Architecture

```
Your Handler
    ?
ObservableRequestHandler (Slck.Envelope.MediatR)
    ?
IRequestHandler (Official MediatR)
    ?
MediatR Pipeline
    ?
ObservabilityPipelineBehavior (Slck.Envelope.MediatR)
    ?
Your HandleAsync() with automatic OTEL + Serilog
```

---

## Benefits

? **No forking needed** - Use official MediatR  
? **Automatic instrumentation** - OTEL + Serilog for all requests  
? **Zero config** - Works out of the box  
? **Compatible** - Works with MediatR.Extensions, FluentValidation.MediatR, etc.  
? **Optional** - Install only if you need MediatR integration  

---

## License

MIT - same as MediatR and Slck.Envelope
