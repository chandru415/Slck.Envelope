# ? Quick Answer: MediatR Registration

## Your Question

> "Can we implement like this: `services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies(...); });`?"

## Answer

? **YES! You have THREE ways to do it:**

---

## Option 1: Standard MediatR Pattern (What You Asked)

```csharp
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        cfg.RegisterServicesFromAssemblies(
            typeof(Ping).Assembly,
            Assembly.GetExecutingAssembly());
    });
```

**This is EXACTLY like standard MediatR, but with automatic OTEL + Serilog!**

---

## Option 2: Even Simpler (Pass Assemblies Directly)

```csharp
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    typeof(Ping).Assembly,
    Assembly.GetExecutingAssembly());
```

**Same result, less code!**

---

## Option 3: Add to Existing MediatR Setup

```csharp
// If you already have this:
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(Ping).Assembly);
});

// Just add this:
builder.Services.AddMediatRObservability(builder.Configuration);
```

**For existing projects with MediatR already registered!**

---

## What You Get (All Options)

- ? MediatR registered
- ? Handlers from assemblies registered
- ? **Automatic OTEL + Serilog for ALL handlers**
- ? Configuration from appsettings.json
- ? No changes to your handler code

---

## Updated Files

1. ? `SlckEnvelopeMediatRExtensions.cs` - Added assembly overloads
2. ? `Program.cs` - Shows all three patterns
3. ? `docs/MEDIATR_REGISTRATION_PATTERNS.md` - Complete guide

---

## Example Usage

```csharp
using System.Reflection;

// ? Works exactly like you want!
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        cfg.RegisterServicesFromAssemblies(
            typeof(Program).Assembly,
            typeof(CreateOrderHandler).Assembly);
        
        // Add more behaviors if needed
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });
```

**Your MediatR handlers get automatic OTEL + Serilog with ZERO code changes!** ??
