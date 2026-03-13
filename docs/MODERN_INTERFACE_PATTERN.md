# ?? Modern Interface-Based Observability Pattern

## Overview

Following .NET best practices (like MediatR), `Slck.Envelope.Observability` now provides a **modern, interface-based architecture** that supports:

? **Multiple inheritance** via interfaces  
? **Composition over inheritance** - base classes are optional  
? **Clear contracts** - interfaces define capabilities  
? **Flexible integration** - works with MediatR, Wolverine, or standalone  

---

## ??? Architecture

### Core Interfaces

```csharp
// Primary handler interface - defines observability contract
public interface IObservableHandler<TResult>
{
    ILogger Logger { get; }
    ActivitySource ActivitySource { get; }
    Task<IResult> HandleAsync();
}

// Marker interfaces for operation type
public interface IObservableCommand<TResult> : IRequest<IResult> 
{
    Task<IResult> ExecuteAsync();
}

public interface IObservableQuery<TResult> : IRequest<IResult>
{
    Task<IResult> ExecuteAsync();
}
```

### Execution Engine

```csharp
// Static executor - provides observability without inheritance
public static class ObservableHandlerExecutor
{
    public static Task<IResult> ExecuteCommandAsync<THandler>(THandler handler)
        where THandler : IObservableHandler<IResult>;
        
    public static Task<IResult> ExecuteQueryAsync<THandler>(THandler handler)
        where THandler : IObservableHandler<IResult>;
}
```

### Optional Base Classes

```csharp
// Optional convenience base class (composition still via interfaces)
public abstract class ObservableCommand<TResult> 
    : IObservableCommand<TResult>, IObservableHandler<IResult>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public async Task<IResult> ExecuteAsync() 
        => await ObservableHandlerExecutor.ExecuteCommandAsync(this);
    
    public abstract Task<IResult> HandleAsync();
}
```

---

## ? Usage Patterns

### Pattern 1: Base Class (Simple, Convenient)

```csharp
// Inherit from base class - simplest approach
public class CreateTicketCommand : ObservableCommand<Ticket>
{
    public CreateTicketCommand(ILogger<CreateTicketCommand> logger, ActivitySource activitySource)
        : base(logger, activitySource) { }
    
    public override async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Creating ticket");
        return Envelope.Ok(ticket);
    }
}
```

### Pattern 2: Interface Only (Maximum Flexibility)

```csharp
// Implement interfaces directly - full control
public class CreateTicketHandler 
    : IObservableHandler<IResult>,
      IObservableCommand<Ticket>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public CreateTicketHandler(ILogger<CreateTicketHandler> logger, ActivitySource activitySource)
    {
        Logger = logger;
        ActivitySource = activitySource;
    }
    
    public async Task<IResult> ExecuteAsync()
        => await ObservableHandlerExecutor.ExecuteCommandAsync(this);
    
    public async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Creating ticket");
        return Envelope.Ok(ticket);
    }
}
```

### Pattern 3: Multiple Interfaces (MediatR Integration)

```csharp
// Implement BOTH MediatR AND Observability interfaces
public class CreateTicketHandler 
    : IRequestHandler<CreateTicketCommand, IResult>,  // MediatR
      IObservableHandler<IResult>,                     // Observability
      IObservableCommand<Ticket>                       // Marker
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    // MediatR entry point
    public async Task<IResult> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        _title = request.Title;
        return await ObservableHandlerExecutor.ExecuteCommandAsync(this);
    }
    
    // Observability implementation
    public async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Creating ticket");
        return Envelope.Ok(ticket);
    }
}
```

---

## ?? Comparison with MediatR Pattern

| Aspect | MediatR | Slck.Envelope Observability |
|--------|---------|---------------------------|
| **Request Interface** | `IRequest<TResponse>` | `IObservableCommand/Query<TResult>` |
| **Handler Interface** | `IRequestHandler<TRequest, TResponse>` | `IObservableHandler<TResult>` |
| **Execution** | `mediator.Send(request)` | `handler.ExecuteAsync()` or `Executor.ExecuteCommandAsync()` |
| **Pipeline** | `IPipelineBehavior<,>` | `ObservableHandlerExecutor` (built-in OTEL/Serilog) |
| **Base Class** | None (interface only) | `ObservableCommand/Query` (optional) |
| **Multiple Inheritance** | ? Interface-based | ? Interface-based |

---

## ?? Modern Design Principles

### 1. **Interface Segregation**
```csharp
// Small, focused interfaces
public interface IObservableHandler<TResult>  // Core capability
public interface IObservableCommand<TResult>   // Command marker
public interface IObservableQuery<TResult>     // Query marker
```

### 2. **Dependency Inversion**
```csharp
// Depend on abstractions, not concretions
public class MyHandler : IObservableHandler<IResult>  // ? Interface
{
    public ILogger Logger { get; }              // ? Interface
    public ActivitySource ActivitySource { get; }  // ? Concrete (framework)
}
```

### 3. **Composition Over Inheritance**
```csharp
// Use executor (composition) instead of base class (inheritance)
public class MyHandler : IObservableHandler<IResult>
{
    public async Task<IResult> ExecuteAsync()
        => await ObservableHandlerExecutor.ExecuteCommandAsync(this);  // Composition
}
```

### 4. **Single Responsibility**
```csharp
// ObservableHandlerExecutor: ONLY handles OTEL + Serilog
// Handler: ONLY handles business logic
// Middleware: ONLY handles HTTP concerns
```

---

## ?? Integration Examples

### With MediatR

```csharp
public class GetTicketHandler 
    : IRequestHandler<GetTicketQuery, IResult>,     // MediatR contract
      IObservableHandler<IResult>,                   // Observability contract
      IObservableQuery<Ticket>                       // Query marker
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public async Task<IResult> Handle(GetTicketQuery request, CancellationToken ct)
    {
        _id = request.Id;
        return await ObservableHandlerExecutor.ExecuteQueryAsync(this);
    }
    
    public async Task<IResult> HandleAsync()
    {
        // Your logic with automatic OTEL + Serilog
        return Envelope.Ok(ticket);
    }
}
```

### With Wolverine

```csharp
public class CreateTicketHandler 
    : IObservableHandler<IResult>,
      IObservableCommand<Ticket>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    // Wolverine convention-based handler
    public async Task<IResult> Handle(CreateTicketCommand command)
    {
        _title = command.Title;
        return await ObservableHandlerExecutor.ExecuteCommandAsync(this);
    }
    
    public async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Creating ticket");
        return Envelope.Created(...);
    }
}
```

### Standalone (No Mediator)

```csharp
public class ListTicketsHandler : IObservableHandler<IResult>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public int Page { get; set; }
    public int PageSize { get; set; }
    
    public async Task<IResult> ExecuteAsync()
        => await ObservableHandlerExecutor.ExecuteQueryAsync(this);
    
    public async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Listing tickets - Page: {Page}", Page);
        return Envelope.Ok(tickets);
    }
}

// Endpoint
app.MapGet("/tickets", async (int page, int pageSize, ListTicketsHandler handler) =>
{
    handler.Page = page;
    handler.PageSize = pageSize;
    return await handler.ExecuteAsync();  // Auto OTEL + Serilog!
});
```

---

## ?? Why This Design?

### ? Advantages

1. **Multiple Inheritance**
   ```csharp
   // Can implement MediatR AND Observability
   public class MyHandler 
       : IRequestHandler<Request, IResult>,
         IObservableHandler<IResult>,
         IMyCustomInterface
   { }
   ```

2. **Testability**
   ```csharp
   // Easy to mock interfaces
   var mockLogger = new Mock<ILogger>();
   var mockActivity = new Mock<ActivitySource>();
   var handler = new MyHandler(mockLogger.Object, mockActivity.Object);
   ```

3. **Flexibility**
   ```csharp
   // Choose: base class OR interface implementation
   public class A : ObservableCommand<T> { }           // Option 1
   public class B : IObservableHandler<T> { }          // Option 2
   ```

4. **Clear Contracts**
   ```csharp
   // Interface defines what you must implement
   public interface IObservableHandler<TResult>
   {
       ILogger Logger { get; }                    // Required
       ActivitySource ActivitySource { get; }     // Required
       Task<IResult> HandleAsync();               // Required
   }
   ```

5. **Separation of Concerns**
   - **Executor**: OTEL + Serilog logic
   - **Handler**: Business logic
   - **Interface**: Contract definition

### ? No More Constraints

Before (class inheritance):
```csharp
// Can't inherit from multiple base classes
public class MyHandler : ObservableCommand<T>, SomeOtherBase  // ? Error!
```

After (interface composition):
```csharp
// Can implement multiple interfaces
public class MyHandler 
    : SomeOtherBase,                         // ? Inherit from one base
      IObservableHandler<IResult>,           // ? Implement interface
      IRequestHandler<Request, IResult>      // ? Implement interface
{ }
```

---

## ?? Migration Guide

### From Old Pattern (Base Class Only)

**Before:**
```csharp
public class CreateTicketCommand : ObservableCommand<Ticket>
{
    protected override async Task<IResult> HandleAsync() { }
}
```

**After (Modern - Interface):**
```csharp
public class CreateTicketCommand 
    : IObservableHandler<IResult>,
      IObservableCommand<Ticket>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    
    public async Task<IResult> ExecuteAsync()
        => await ObservableHandlerExecutor.ExecuteCommandAsync(this);
    
    public async Task<IResult> HandleAsync() { }
}
```

**After (Modern - Base Class Still Supported):**
```csharp
// Base class still works - now implements IObservableHandler internally
public class CreateTicketCommand : ObservableCommand<Ticket>
{
    public override async Task<IResult> HandleAsync() { }  // Now public!
}
```

---

## ?? Best Practices

1. **Use interfaces for libraries/frameworks**
   ```csharp
   // When creating reusable handlers
   public class MyHandler : IObservableHandler<IResult> { }
   ```

2. **Use base classes for quick prototypes**
   ```csharp
   // When building fast
   public class QuickHandler : ObservableCommand<T> { }
   ```

3. **Combine with MediatR for large apps**
   ```csharp
   // When you need decoupling + observability
   public class MyHandler 
       : IRequestHandler<Request, IResult>,
         IObservableHandler<IResult> { }
   ```

4. **Keep HandleAsync() focused**
   ```csharp
   public async Task<IResult> HandleAsync()
   {
       // ONLY business logic here
       // OTEL + Serilog handled by executor
   }
   ```

---

## ?? Summary

| Concept | Implementation |
|---------|----------------|
| **Contracts** | `IObservableHandler<T>`, `IObservableCommand<T>`, `IObservableQuery<T>` |
| **Execution** | `ObservableHandlerExecutor.ExecuteCommandAsync()` |
| **Convenience** | `ObservableCommand<T>`, `ObservableQuery<T>` (optional base classes) |
| **Integration** | Implements interfaces alongside `IRequestHandler`, etc. |
| **Pattern** | Interface-based composition (like MediatR) |

**Result**: Modern, flexible, testable observability that works with any architecture! ??
