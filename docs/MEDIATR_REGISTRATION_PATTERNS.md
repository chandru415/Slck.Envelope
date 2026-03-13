# ?? MediatR Registration Patterns with Slck.Envelope

## Overview

You can register MediatR with Slck.Envelope observability in **multiple ways** - choose the pattern that fits your needs!

---

## ?? All Available Registration Patterns

### Pattern 1: Simple Assembly Registration (Recommended)

```csharp
// ? Scan assemblies for handlers - SIMPLEST!
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(SomeOtherClass).Assembly);
```

**Use when**: You have handlers in one or more assemblies and want automatic discovery.

---

### Pattern 2: Custom Configuration Action

```csharp
// ? Full control over MediatR configuration
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        // Standard MediatR registration
        cfg.RegisterServicesFromAssemblies(
            typeof(Program).Assembly,
            Assembly.GetExecutingAssembly());
        
        // Add custom behaviors, validators, etc.
        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });
```

**Use when**: You need custom MediatR configuration (additional behaviors, validators, etc.).

---

### Pattern 3: Without Configuration (Development)

```csharp
// ? Quick setup for development - reads from appsettings.json
builder.Services.AddSlckEnvelopeMediatR(
    typeof(Program).Assembly);
```

**Use when**: Quick prototyping, development, testing.

---

### Pattern 4: Add to Existing MediatR Setup

```csharp
// ? You already have MediatR registered - just add observability!
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Add observability to existing setup
builder.Services.AddMediatRObservability(builder.Configuration);
```

**Use when**: You already have MediatR registered and just want to add observability.

---

## ?? Complete Examples

### Example 1: Standard MediatR Pattern (What You Asked About)

```csharp
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ? YES! You can do this - exactly like standard MediatR!
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        cfg.RegisterServicesFromAssemblies(
            typeof(Ping).Assembly,
            Assembly.GetExecutingAssembly());
    });

var app = builder.Build();
app.Run();
```

**What you get**:
- ? MediatR registered
- ? Handlers from specified assemblies registered
- ? Automatic OTEL + Serilog for all handlers
- ? Configuration from appsettings.json

---

### Example 2: Multiple Assemblies (Cleaner Syntax)

```csharp
// ? Even simpler - pass assemblies directly!
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    typeof(Ping).Assembly,
    Assembly.GetExecutingAssembly(),
    typeof(SomeOtherHandler).Assembly);
```

**What you get**:
- ? Same as Example 1
- ? Cleaner syntax (no cfg => lambda)

---

### Example 3: Multi-Project Solution

```csharp
// Project structure:
// - MyApp.API (web project)
// - MyApp.Application (handlers)
// - MyApp.Domain (commands/queries)

// In Program.cs
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    typeof(Program).Assembly,                    // API assembly
    typeof(CreateOrderHandler).Assembly,         // Application assembly
    typeof(CreateOrderCommand).Assembly);        // Domain assembly
```

---

### Example 4: With Custom Behaviors

```csharp
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        // Register handlers
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        
        // Add custom behaviors (run in order)
        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));           // 1. Custom logging
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));        // 2. FluentValidation
        cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));       // 3. DB transactions
        // ObservabilityPipelineBehavior added automatically by Slck.Envelope
    });
```

**Pipeline order**:
```
Request
  ?
LoggingBehavior
  ?
ValidationBehavior
  ?
TransactionBehavior
  ?
ObservabilityPipelineBehavior (automatic OTEL + Serilog)
  ?
Your Handler
```

---

### Example 5: Existing MediatR Setup

```csharp
// You already have this:
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Just add observability!
builder.Services.AddMediatRObservability(builder.Configuration);
```

**Use when**: You're adding Slck.Envelope to an existing project.

---

## ?? Comparison of All Patterns

| Pattern | Syntax | Use Case |
|---------|--------|----------|
| **Simple Assembly** | `AddSlckEnvelopeMediatR(config, assemblies)` | Single/multiple assemblies, simple setup |
| **Custom Config** | `AddSlckEnvelopeMediatR(config, cfg => {...})` | Need custom behaviors, validators |
| **No Config** | `AddSlckEnvelopeMediatR(assemblies)` | Quick dev/testing |
| **Add to Existing** | `AddMediatRObservability(config)` | Already have MediatR registered |

---

## ?? Real-World Example

```csharp
using System.Reflection;
using FluentValidation;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// ? Production setup with everything
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        // 1. Register handlers from multiple assemblies
        cfg.RegisterServicesFromAssemblies(
            Assembly.GetExecutingAssembly(),           // Current API
            typeof(CreateOrderHandler).Assembly,       // Application layer
            typeof(CreateOrderCommand).Assembly);      // Domain layer
        
        // 2. Add FluentValidation
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        
        // 3. Add transaction support
        cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        
        // ObservabilityPipelineBehavior automatically added by Slck.Envelope!
    });

// FluentValidation setup
builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderCommandValidator).Assembly);

var app = builder.Build();

// Use MediatR as normal
app.MapPost("/orders", async (CreateOrderCommand command, IMediator mediator) =>
{
    // ? Automatic validation
    // ? Automatic transaction
    // ? Automatic OTEL + Serilog (from Slck.Envelope)
    return await mediator.Send(command);
});

app.Run();
```

---

## ? Answer to Your Question

### Your Question:
> "Can we implement like this: `services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies(...); });`?"

### Answer:
? **YES! Absolutely!**

```csharp
// ? Method 1: Use Slck.Envelope's method with lambda
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        cfg.RegisterServicesFromAssemblies(
            typeof(Ping).Assembly,
            Assembly.GetExecutingAssembly());
    });

// ? Method 2: Even simpler - pass assemblies directly
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    typeof(Ping).Assembly,
    Assembly.GetExecutingAssembly());

// ? Method 3: Add to existing MediatR setup
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Ping).Assembly,
        Assembly.GetExecutingAssembly());
});
builder.Services.AddMediatRObservability(builder.Configuration);
```

**All three methods work!** Choose based on your preference. ??

---

## ?? Recommended Pattern

### For Most Projects:

```csharp
// ? RECOMMENDED: Simple, clean, powerful
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(CreateOrderHandler).Assembly);
```

### For Complex Projects:

```csharp
// ? RECOMMENDED: Full control
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
    });
```

---

## ?? Summary

**Your implementation already supports all standard MediatR registration patterns!**

**Available overloads**:
1. ? `AddSlckEnvelopeMediatR(IConfiguration, Action<MediatRServiceConfiguration>)` - Full control
2. ? `AddSlckEnvelopeMediatR(IConfiguration, params Assembly[])` - Simple assembly scan
3. ? `AddSlckEnvelopeMediatR(params Assembly[])` - No config needed
4. ? `AddMediatRObservability(IConfiguration)` - Add to existing setup

**Choose the pattern that fits your needs!** ??
