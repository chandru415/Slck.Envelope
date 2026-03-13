# Slck.Envelope.Decorators

Wrapper/decorator pattern for adding **automatic OTEL tracing** and **Serilog logging** to **any class or method** without modifying existing code.

## ?? Purpose

This package provides a simple way to add observability to:
- Existing services/classes
- Third-party libraries
- Legacy code
- Any method you want to trace

**No need to modify existing code** - just wrap it!

---

## ?? Installation

```bash
dotnet add package Slck.Envelope.Decorators
```

---

## ?? Usage Patterns

### Pattern 1: Inherit from `ObservableService` (Recommended)

```csharp
using Slck.Envelope.Decorators;

public class TicketService : ObservableService
{
    private readonly ITicketRepository _repository;

    public TicketService(
        ILogger<TicketService> logger,
        ActivitySource activitySource,
        ITicketRepository repository)
        : base(logger, activitySource)
    {
        _repository = repository;
    }

    public async Task<Ticket> CreateTicketAsync(string title)
    {
        // Automatic OTEL + Serilog!
        return await ExecuteObservableAsync(
            "CreateTicket",
            async () =>
            {
                // Your business logic
                var ticket = new Ticket { Title = title };
                await _repository.SaveAsync(ticket);
                return ticket;
            },
            new Dictionary<string, object>
            {
                ["ticket.title"] = title  // Custom OTEL tags
            });
    }

    public void DeleteTicket(string id)
    {
        // Sync version
        ExecuteObservable(
            "DeleteTicket",
            () => _repository.Delete(id),
            new Dictionary<string, object> { ["ticket.id"] = id });
    }
}
```

### Pattern 2: Use `ObservableExecutor` Directly (Wrap Anything)

```csharp
using Slck.Envelope.Decorators;

public class LegacyService
{
    private readonly ILogger _logger;
    private readonly ActivitySource _activitySource;

    public LegacyService(ILogger<LegacyService> logger, ActivitySource activitySource)
    {
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task<Result> ProcessDataAsync(Data data)
    {
        // Wrap existing method with observability
        return await ObservableExecutor.ExecuteAsync(
            _logger,
            _activitySource,
            "ProcessData",
            async () =>
            {
                // Call existing logic
                return await SomeExistingMethod(data);
            },
            new Dictionary<string, object>
            {
                ["data.id"] = data.Id,
                ["data.size"] = data.Size
            });
    }
}
```

### Pattern 3: Wrap Third-Party Code

```csharp
public class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ActivitySource _activitySource;

    public async Task<Response> CallExternalApiAsync(Request request)
    {
        // Wrap third-party HTTP call
        return await ObservableExecutor.ExecuteAsync(
            _logger,
            _activitySource,
            "ExternalApi.Call",
            async () =>
            {
                // Third-party library code
                var response = await _httpClient.PostAsJsonAsync("/api/endpoint", request);
                return await response.Content.ReadFromJsonAsync<Response>();
            },
            new Dictionary<string, object>
            {
                ["api.endpoint"] = "/api/endpoint",
                ["request.id"] = request.Id
            });
    }
}
```

---

## ?? What You Get Automatically

### OTEL Trace Spans

```
Trace: 8d3c4b2a1f6e5d7c
?? Span: POST /tickets
?  ?? Span: CreateTicket
?     Tags:
?       - ticket.title: "Fix bug"
?     Status: Ok
```

### Serilog Logs

```json
{
  "Message": "Executing operation: CreateTicket",
  "OperationName": "CreateTicket",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4"
}
{
  "Message": "Operation CreateTicket completed successfully",
  "OperationName": "CreateTicket",
  "TraceId": "8d3c4b2a1f6e5d7c"
}
```

---

## ?? Configuration

### appsettings.json

```json
{
  "SlckEnvelope": {
    "Observability": {
      "Enabled": true,
      "ActivitySourceName": "MyApp",
      "ActivitySourceVersion": "1.0.0"
    }
  }
}
```

### Registration

```csharp
using Slck.Envelope.Observability;

var builder = WebApplication.CreateBuilder(args);

// Load from appsettings.json
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

// Register your services
builder.Services.AddScoped<TicketService>();
```

---

## ?? Advanced Usage

### Custom Tags

```csharp
await ExecuteObservableAsync(
    "ImportData",
    async () => await Import(),
    new Dictionary<string, object>
    {
        ["user.id"] = userId,
        ["batch.size"] = 1000,
        ["source"] = "csv"
    });
```

### Nested Operations

```csharp
public async Task<Order> ProcessOrderAsync(Order order)
{
    return await ExecuteObservableAsync("ProcessOrder", async () =>
    {
        // This creates a child span
        await ExecuteObservableAsync("ValidateOrder", async () =>
        {
            await _validator.ValidateAsync(order);
        });

        // Another child span
        await ExecuteObservableAsync("SaveOrder", async () =>
        {
            await _repository.SaveAsync(order);
        });

        return order;
    });
}
```

Result: Nested OTEL spans!

```
ProcessOrder
?? ValidateOrder
?? SaveOrder
```

---

## ?? When to Use This Package

| Scenario | Use This Package? |
|----------|-------------------|
| **Wrapping existing code** | ? Yes |
| **Third-party libraries** | ? Yes |
| **Legacy services** | ? Yes |
| **Quick observability** | ? Yes |
| **CQRS pattern** | ? Use `Slck.Envelope.Observability` |
| **MediatR integration** | ? Use `Slck.Envelope.MediatR` |

---

## ?? Comparison with Other Packages

| Package | Use Case |
|---------|----------|
| `Slck.Envelope` | Core envelope responses |
| `Slck.Envelope.Observability` | CQRS with auto OTEL/Serilog |
| `Slck.Envelope.MediatR` | MediatR with auto OTEL/Serilog |
| `Slck.Envelope.Decorators` | Wrap any class/method |

---

## ?? Testing

```csharp
[Fact]
public async Task Service_LogsOperation()
{
    var mockLogger = new Mock<ILogger<TicketService>>();
    var activitySource = new ActivitySource("Test");
    var service = new TicketService(mockLogger.Object, activitySource, repo);

    await service.CreateTicketAsync("Test");

    mockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Executing operation")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

---

## ?? Summary

**`Slck.Envelope.Decorators`** provides:
? Wrapper pattern for any code  
? No code modification needed  
? Automatic OTEL tracing  
? Automatic Serilog logging  
? Simple API: `ExecuteObservable()` / `ExecuteObservableAsync()`  
? Works with existing classes  

Perfect for adding observability to code you can't or don't want to modify!
