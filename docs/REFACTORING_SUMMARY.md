# ? Refactored: Modern Interface-Based Observability

## What Changed?

You requested a **modern, MediatR-style interface-based design** instead of class inheritance. Here's what was refactored:

---

## ?? New Architecture

### Before (Class Inheritance Only)
```csharp
// ? Old: Forced to inherit from base class
public class MyHandler : ObservableCommand<T>
{
    protected override async Task<IResult> HandleAsync() { }
}

// ? Problem: Can't inherit from multiple classes
public class MyHandler : ObservableCommand<T>, SomeOtherBase  // Error!
```

### After (Interface-Based, Like MediatR)
```csharp
// ? New: Implement interfaces (multiple inheritance supported)
public class MyHandler 
    : IObservableHandler<IResult>,           // Observability contract
      IObservableCommand<T>,                  // Marker interface
      IRequestHandler<Request, IResult>       // MediatR (optional)
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public async Task<IResult> ExecuteAsync()
        => await ObservableHandlerExecutor.ExecuteCommandAsync(this);
    
    public async Task<IResult> HandleAsync()
    {
        // Your business logic with automatic OTEL + Serilog
    }
}

// ? Still supported: Base class for convenience
public class SimpleHandler : ObservableCommand<T>
{
    public override async Task<IResult> HandleAsync() { }
}
```

---

## ?? New Components

| Component | Type | Purpose |
|-----------|------|---------|
| `IObservableHandler<T>` | Interface | Core contract for handlers with Logger + ActivitySource |
| `IObservableCommand<T>` | Interface | Marker for command operations |
| `IObservableQuery<T>` | Interface | Marker for query operations |
| `ObservableHandlerExecutor` | Static Class | Executes handlers with automatic OTEL + Serilog |
| `ObservableCommand<T>` | Abstract Class | Optional base class (now implements interfaces) |
| `ObservableQuery<T>` | Abstract Class | Optional base class (now implements interfaces) |

---

## ?? Design Comparison

### Like MediatR

| MediatR | Slck.Envelope Observability |
|---------|----------------------------|
| `IRequest<TResponse>` | `IObservableCommand<T>` / `IObservableQuery<T>` |
| `IRequestHandler<TRequest, TResponse>` | `IObservableHandler<TResult>` |
| `mediator.Send(request)` | `handler.ExecuteAsync()` or `Executor.ExecuteCommandAsync()` |
| No base class (interface only) | `ObservableCommand/Query` (optional base class) |
| ? Multiple inheritance | ? Multiple inheritance |

---

## ? Key Features

### 1. **Multiple Interface Implementation**
```csharp
public class CreateTicketHandler 
    : IRequestHandler<CreateTicketCommand, IResult>,  // MediatR
      IObservableHandler<IResult>,                     // Observability
      IMyCustomInterface,                              // Your interface
      IDisposable                                      // Framework interface
{
    // Implement all interfaces
}
```

### 2. **Executor Pattern (Composition)**
```csharp
// Instead of base class calling internal methods,
// use static executor for OTEL + Serilog
public async Task<IResult> ExecuteAsync()
{
    return await ObservableHandlerExecutor.ExecuteCommandAsync(this);
}
```

### 3. **Clear Contracts**
```csharp
public interface IObservableHandler<TResult>
{
    ILogger Logger { get; }              // Required
    ActivitySource ActivitySource { get; }  // Required
    Task<IResult> HandleAsync();         // Required
}
```

### 4. **Flexible Usage**
```csharp
// Option 1: Pure interfaces (maximum flexibility)
public class A : IObservableHandler<IResult> { }

// Option 2: Base class (convenience)
public class B : ObservableCommand<T> { }

// Option 3: Combine both
public class C : ObservableCommand<T>, IRequestHandler<Req, Res> { }
```

---

## ?? Files Changed

### Core Library (`src/Slck.Envelope/Observability/`)

| File | Change |
|------|--------|
| `IObservableHandler.cs` | **NEW** - Core handler interface |
| `ObservableHandlerExecutor.cs` | **NEW** - Static executor for OTEL + Serilog |
| `ObservableCommand.cs` | **REFACTORED** - Now implements `IObservableHandler` |
| `ObservableQuery.cs` | **REFACTORED** - Now implements `IObservableHandler` |
| `IObservableCommand.cs` | No change (marker interface) |
| `IObservableQuery.cs` | No change (marker interface) |

### Sample App (`samples/sample.api/`)

| File | Purpose |
|------|---------|
| `Commands/CreateTicketCommand.cs` | Base class example |
| `Queries/GetTicketByIdQuery.cs` | Base class example |
| `Queries/GetAllTicketsQuery.cs` | Base class with pagination |
| `Modern/Queries/ListTicketsQueryHandler.cs` | **NEW** - Pure interface example |
| `Program.cs` | Updated to show both patterns |

### Documentation (`docs/`)

| File | Content |
|------|---------|
| `MODERN_INTERFACE_PATTERN.md` | **NEW** - Modern pattern guide |
| `OBSERVABILITY.md` | Existing (still valid) |
| `QUICK_START_CQRS.md` | Existing (still valid) |
| `MEDIATR_INTEGRATION.md` | Existing (shows multiple interfaces) |
| `ARCHITECTURE_DIAGRAM.md` | Existing (shows flow) |

---

## ?? Usage Examples

### Example 1: Pure Interface (Modern, Flexible)

```csharp
public class GetTicketHandler 
    : IObservableHandler<IResult>,
      IObservableQuery<Ticket>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public GetTicketHandler(ILogger<GetTicketHandler> logger, ActivitySource activitySource)
    {
        Logger = logger;
        ActivitySource = activitySource;
    }
    
    public async Task<IResult> ExecuteAsync()
        => await ObservableHandlerExecutor.ExecuteQueryAsync(this);
    
    public async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Fetching ticket");
        Activity.Current?.SetTag("ticket.id", id);
        return Envelope.Ok(ticket);
    }
}
```

### Example 2: Base Class (Convenient)

```csharp
public class GetTicketQuery : ObservableQuery<Ticket>
{
    public GetTicketQuery(ILogger<GetTicketQuery> logger, ActivitySource activitySource)
        : base(logger, activitySource) { }
    
    public override async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Fetching ticket");
        return Envelope.Ok(ticket);
    }
}
```

### Example 3: With MediatR (Multiple Interfaces)

```csharp
public class GetTicketHandler 
    : IRequestHandler<GetTicketQuery, IResult>,   // MediatR
      IObservableHandler<IResult>,                 // Observability
      IObservableQuery<Ticket>                     // Marker
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    // MediatR entry point
    public async Task<IResult> Handle(GetTicketQuery request, CancellationToken ct)
    {
        _id = request.Id;
        return await ObservableHandlerExecutor.ExecuteQueryAsync(this);
    }
    
    // Observability implementation
    public async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Fetching ticket");
        return Envelope.Ok(ticket);
    }
}
```

---

## ?? Benefits of Refactoring

| Benefit | Description |
|---------|-------------|
| **Multiple Inheritance** | Implement multiple interfaces (MediatR, Observability, custom) |
| **Testability** | Easy to mock `ILogger` and `ActivitySource` |
| **Flexibility** | Choose base class OR interface implementation |
| **Clear Contracts** | Interfaces define required properties and methods |
| **Modern Pattern** | Follows .NET best practices (like MediatR, Wolverine) |
| **Composition** | Executor pattern instead of inheritance chain |

---

## ?? Testing Example

```csharp
[Fact]
public async Task Handler_LogsInformation_WhenExecuted()
{
    // Arrange
    var mockLogger = new Mock<ILogger<MyHandler>>();
    var activitySource = new ActivitySource("Test");
    var handler = new MyHandler(mockLogger.Object, activitySource);
    
    // Act
    var result = await handler.ExecuteAsync();
    
    // Assert
    mockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Executing")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

---

## ?? Migration Path

### From Old Code

**Before:**
```csharp
public class MyHandler : ObservableCommand<T>
{
    protected override async Task<IResult> HandleAsync() { }
}
```

**After (Option 1 - Keep Base Class):**
```csharp
public class MyHandler : ObservableCommand<T>
{
    public override async Task<IResult> HandleAsync() { }  // Now public!
}
```

**After (Option 2 - Use Interfaces):**
```csharp
public class MyHandler : IObservableHandler<IResult>, IObservableCommand<T>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public async Task<IResult> ExecuteAsync()
        => await ObservableHandlerExecutor.ExecuteCommandAsync(this);
    
    public async Task<IResult> HandleAsync() { }
}
```

---

## ? Summary

**You asked for**: Modern, interface-based design like MediatR with multiple inheritance support.

**You got**:
? `IObservableHandler<T>` interface (core contract)  
? `ObservableHandlerExecutor` (composition over inheritance)  
? Multiple interface implementation support  
? Optional base classes (`ObservableCommand`, `ObservableQuery`)  
? MediatR integration pattern  
? Clear separation of concerns  
? Modern .NET best practices  

**Result**: Production-ready, flexible, testable, and modern CQRS observability! ??
