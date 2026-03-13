# ?? MediatR + Slck.Envelope Observability Integration

## Overview

This guide shows how to combine **MediatR** with **Slck.Envelope Observability** to get the best of both worlds:
- **MediatR**: Decoupled request/response, pipeline behaviors, validation
- **Slck.Envelope Observability**: Automatic OTEL tracing and Serilog logging

## ?? Architecture

```
HTTP Request
    ?
Envelope Middleware (RequestId, Error Handling)
    ?
Endpoint (calls mediator.Send())
    ?
MediatR Pipeline Behaviors (Logging, Validation, Caching, etc.)
    ?
Handler (implements both IRequestHandler AND ObservableQuery/Command)
    ?
ObservableQuery/Command (creates OTEL span, Serilog scope)
    ?
HandleAsync() - Your Business Logic
    ?
Response (wrapped in Envelope format)
```

## ? What You Get

| Feature | Source | What It Does |
|---------|--------|--------------|
| **Distributed Tracing** | ObservableQuery/Command | OTEL spans for every operation |
| **Structured Logging** | ObservableQuery/Command | Serilog with TraceId/SpanId |
| **Request Validation** | MediatR + FluentValidation | Validate before handler |
| **Pipeline Behaviors** | MediatR | Logging, caching, transactions, etc. |
| **Decoupling** | MediatR | Endpoints don't know about handlers |
| **Error Handling** | Envelope Middleware | Consistent error responses |
| **Request/Response** | MediatR | Strongly-typed messages |

## ?? Implementation Guide

### 1. Install Required Packages

```bash
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### 2. Register Services

```csharp
// Register Observability
builder.Services.AddSlckEnvelopeObservability(options =>
{
    options.ActivitySourceName = "MyAPI";
});

// Register MediatR with behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### 3. Create MediatR Request

```csharp
using MediatR;

public record GetTicketByIdRequest(string Id) : IRequest<IResult>;
```

### 4. Create Handler (Combines MediatR + Observability)

```csharp
using System.Diagnostics;
using MediatR;
using Slck.Envelope.Observability;

public class GetTicketByIdHandler 
    : ObservableQuery<Ticket>,           // ? Slck.Envelope Observability
      IRequestHandler<GetTicketByIdRequest, IResult>  // ? MediatR
{
    private readonly ITicketRepository _repository;
    private string _ticketId = string.Empty;

    public GetTicketByIdHandler(
        ILogger<GetTicketByIdHandler> logger,
        ActivitySource activitySource,
        ITicketRepository repository)
        : base(logger, activitySource)
    {
        _repository = repository;
    }

    // ? MediatR entry point
    public async Task<IResult> Handle(GetTicketByIdRequest request, CancellationToken ct)
    {
        _ticketId = request.Id;
        return await ExecuteAsync(); // Calls ObservableQuery
    }

    // ? Observability entry point - automatic OTEL + Serilog
    protected override async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Fetching ticket: {TicketId}", _ticketId);
        
        var ticket = await _repository.GetByIdAsync(_ticketId);
        
        if (ticket is null)
            return Envelope.NotFound($"Ticket '{_ticketId}' not found");
        
        Activity.Current?.SetTag("ticket.id", ticket.Id);
        return Envelope.Ok(ticket);
    }
}
```

### 5. Create Validator (Optional)

```csharp
using FluentValidation;

public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters");
    }
}
```

### 6. Use in Endpoints

```csharp
app.MapGet("/ticket/{id}", async (string id, IMediator mediator) =>
{
    return await mediator.Send(new GetTicketByIdRequest(id));
});

app.MapPost("/ticket", async (CreateTicketRequest request, IMediator mediator) =>
{
    return await mediator.Send(request); // Validation happens automatically
});
```

## ?? Execution Flow Example

### Request: `POST /ticket` with `{ "title": "Fix bug" }`

```
1. HTTP Request arrives
   ?
2. RequestIdMiddleware sets X-Request-Id
   ?
3. Endpoint calls mediator.Send(new CreateTicketRequest("Fix bug"))
   ?
4. MediatR LoggingBehavior
   Log: "MediatR Pipeline: Handling CreateTicketRequest"
   ?
5. MediatR ValidationBehavior
   FluentValidation runs ? passes
   ?
6. CreateTicketRequestHandler.Handle() called
   Sets _title = "Fix bug"
   Calls ExecuteAsync()
   ?
7. ObservableCommand.ExecuteAsync()
   Creates OTEL span: "Command.CreateTicketRequestHandler"
   Creates Serilog scope with TraceId, SpanId, CommandName
   Log: "Executing command: CreateTicketRequestHandler"
   ?
8. CreateTicketRequestHandler.HandleAsync()
   Log: "Creating ticket with title: Fix bug"
   Creates ticket
   Activity.SetTag("ticket.id", "abc-123")
   Log: "Ticket created successfully"
   Returns Envelope.Created(...)
   ?
9. ObservableCommand completes
   Log: "Command CreateTicketRequestHandler completed successfully"
   OTEL span marked as OK
   ?
10. MediatR LoggingBehavior completes
    Log: "MediatR Pipeline: CreateTicketRequest completed"
    ?
11. Response returned
    Status: 201 Created
    Body: { "success": true, "data": { "id": "abc-123", ... }, "requestId": "..." }
```

## ?? What Logs Look Like

### With Serilog Structured Logging

```json
{
  "Timestamp": "2024-01-15T14:32:15Z",
  "Level": "Information",
  "Message": "MediatR Pipeline: Handling CreateTicketRequest",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4e5f6",
  "RequestId": "8d3c4b2a1f6e5d7c"
}
{
  "Timestamp": "2024-01-15T14:32:15Z",
  "Level": "Information",
  "Message": "Executing command: CreateTicketRequestHandler",
  "CommandName": "CreateTicketRequestHandler",
  "CommandType": "Command",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "b2c3d4e5f6a7"
}
{
  "Timestamp": "2024-01-15T14:32:15Z",
  "Level": "Information",
  "Message": "Creating ticket with title: Fix bug",
  "CommandName": "CreateTicketRequestHandler",
  "TraceId": "8d3c4b2a1f6e5d7c"
}
```

## ?? Advanced: Pipeline Behaviors

### Logging Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("MediatR: Handling {RequestName}", requestName);
        
        var response = await next();
        
        _logger.LogInformation("MediatR: {RequestName} completed", requestName);
        return response;
    }
}
```

### Validation Behavior (with Envelope Integration)

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var failures = await ValidateAsync(request);
        
        if (failures.Any())
        {
            var errors = failures.GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
            
            // Return Envelope error response
            if (typeof(TResponse) == typeof(IResult))
            {
                var result = Envelope.UnprocessableEntity(errors, "Validation failed");
                return (TResponse)(object)result;
            }
        }
        
        return await next();
    }
}
```

### Caching Behavior

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Only cache queries, not commands
        if (!typeof(TRequest).Name.EndsWith("Query"))
            return await next();

        var cacheKey = $"{typeof(TRequest).Name}:{JsonSerializer.Serialize(request)}";
        
        if (_cache.TryGetValue(cacheKey, out TResponse? cached))
        {
            Activity.Current?.SetTag("cache.hit", true);
            return cached!;
        }

        var response = await next();
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        Activity.Current?.SetTag("cache.hit", false);
        return response;
    }
}
```

## ?? Migration Path

### From Direct Handlers to MediatR

**Before (Direct ObservableQuery):**
```csharp
builder.Services.AddScoped<GetTicketByIdQuery>();

app.MapGet("/ticket/{id}", async (string id, GetTicketByIdQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync();
});
```

**After (MediatR + ObservableQuery):**
```csharp
// No manual registration needed - MediatR finds handlers

app.MapGet("/ticket/{id}", async (string id, IMediator mediator) =>
{
    return await mediator.Send(new GetTicketByIdRequest(id));
});
```

## ?? Benefits Comparison

| Aspect | Without MediatR | With MediatR | Combined |
|--------|-----------------|--------------|----------|
| **Decoupling** | ? Endpoint knows handler | ? Endpoint uses IMediator | ? Best |
| **OTEL Tracing** | ? From ObservableQuery | ? Manual | ? Automatic |
| **Serilog** | ? From ObservableQuery | ? Manual | ? Automatic |
| **Validation** | ? Manual in handler | ? Pipeline behavior | ? Best |
| **Caching** | ? Manual | ? Pipeline behavior | ? Best |
| **Testing** | ?? Mock query class | ? Mock IMediator | ? Best |

## ?? Best Practices

1. **Use MediatR for decoupling** - Endpoints send requests, don't know about handlers
2. **Use ObservableQuery/Command for observability** - Automatic OTEL + Serilog
3. **Order pipeline behaviors carefully**:
   ```csharp
   cfg.AddBehavior<LoggingBehavior>();      // First - logs everything
   cfg.AddBehavior<ValidationBehavior>();   // Second - validate early
   cfg.AddBehavior<CachingBehavior>();      // Third - cache valid requests
   cfg.AddBehavior<TransactionBehavior>();  // Last - wraps execution
   ```
4. **Keep handlers focused** - One responsibility per handler
5. **Use FluentValidation** - Declarative validation rules
6. **Add custom OTEL tags** - Use `Activity.Current?.SetTag()` for context

## ?? Testing

### Unit Test Handler
```csharp
[Fact]
public async Task Handle_ReturnsTicket_WhenExists()
{
    var logger = new Mock<ILogger<GetTicketByIdHandler>>();
    var activitySource = new ActivitySource("Test");
    var repo = new Mock<ITicketRepository>();
    
    var handler = new GetTicketByIdHandler(logger.Object, activitySource, repo.Object);
    
    var result = await handler.Handle(new GetTicketByIdRequest("123"), CancellationToken.None);
    
    // Assert result
}
```

### Integration Test with MediatR
```csharp
[Fact]
public async Task Endpoint_ReturnsTicket_ViaMediatR()
{
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    var response = await client.GetAsync("/ticket/123");
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    // Assert envelope response
}
```

## ?? Summary

**When you derive from both `ObservableQuery`/`ObservableCommand` AND implement `IRequestHandler`:**

? **You get MediatR benefits:**
- Decoupled architecture
- Pipeline behaviors (validation, caching, logging, transactions)
- Testability via `IMediator` mocking
- Request/response pattern

? **You get Observability benefits:**
- Automatic OTEL distributed tracing
- Automatic Serilog structured logging with TraceId/SpanId
- Error tracking with trace correlation
- Zero boilerplate instrumentation

? **Combined flow:**
```
HTTP ? Envelope Middleware ? Endpoint ? MediatR ? Behaviors ? Handler ? ObservableQuery ? OTEL/Serilog ? Business Logic
```

This gives you **production-ready, observable, maintainable CQRS** with minimal code! ??
