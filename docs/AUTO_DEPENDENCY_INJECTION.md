# ?? Ultra-Simple Approach: Auto Dependency Injection

## Problem

Standard approach requires injecting too many dependencies:

```csharp
// ? TOO VERBOSE - 4 constructor parameters every time!
public class MyQuery(
    ILogger<MyQuery> logger,           // ? Boilerplate
    ActivitySource activitySource,     // ? Boilerplate
    SlckEnvelopeObservabilityOptions? options, // ? Boilerplate
    List<Ticket> tickets)              // ? Only this is needed!
    : ObservableQuery<Ticket>(logger, activitySource, options)
{
    // ...
}
```

## Solution

New `Auto*` base classes that resolve dependencies automatically:

```csharp
// ? CLEAN - Only 2 constructor parameters!
public class MyQuery(
    IHttpContextAccessor httpContextAccessor,  // ? Always needed
    List<Ticket> tickets)                       // ? Your dependency
    : AutoObservableQuery<Ticket>(httpContextAccessor)
{
    // Logger, ActivitySource, Options available automatically!
}
```

---

## ?? Two Approaches Available

| Approach | Base Class | Constructor Parameters | When to Use |
|----------|------------|------------------------|-------------|
| **Standard** | `ObservableCommand/Query<T>` | `ILogger`, `ActivitySource`, `Options`, + yours | Explicit DI, testing, full control |
| **Auto** ? | `AutoObservableCommand/Query<T>` | `IHttpContextAccessor` + yours | Quick development, less boilerplate |

---

## ?? Auto Approach (Recommended for Quick Development)

### Setup (ONE LINE)

```csharp
// Registers IHttpContextAccessor automatically
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);
```

### Create Query

```csharp
using Microsoft.AspNetCore.Http;
using Slck.Envelope.Observability;

public class GetTicketQuery : AutoObservableQuery<Ticket>
{
    private readonly List<Ticket> _tickets;

    // ? ONLY inject: IHttpContextAccessor + YOUR dependencies
    public GetTicketQuery(
        IHttpContextAccessor httpContextAccessor,
        List<Ticket> tickets)
        : base(httpContextAccessor)
    {
        _tickets = tickets;
    }

    public string TicketId { get; set; } = "";

    public override async Task<IResult> HandleAsync()
    {
        // ? Logger available - resolved automatically!
        // ? ActivitySource available - resolved automatically!
        // ? Options available - resolved automatically!
        
        Logger.LogInformation("Searching for ticket: {Id}", TicketId);
        
        var ticket = _tickets.FirstOrDefault(t => t.Id == TicketId);
        
        return ticket != null 
            ? Envelope.Ok(ticket)
            : Envelope.NotFound($"Ticket '{TicketId}' not found");
    }
}
```

### Create Command

```csharp
public class CreateTicketCommand : AutoObservableCommand<Ticket>
{
    private readonly List<Ticket> _tickets;

    // ? ONLY inject: IHttpContextAccessor + YOUR dependencies
    public CreateTicketCommand(
        IHttpContextAccessor httpContextAccessor,
        List<Ticket> tickets)
        : base(httpContextAccessor)
    {
        _tickets = tickets;
    }

    public string Title { get; set; } = "";

    public override async Task<IResult> HandleAsync()
    {
        // ? All observability dependencies available automatically!
        
        Logger.LogInformation("Creating ticket: {Title}", Title);
        
        var ticket = new Ticket { Id = Guid.NewGuid().ToString(), Title = Title };
        _tickets.Add(ticket);
        
        Logger.LogInformation("Ticket created: {Id}", ticket.Id);
        
        return Envelope.Created($"/ticket/{ticket.Id}", ticket);
    }
}
```

### Register & Use

```csharp
// Register
builder.Services.AddScoped<GetTicketQuery>();
builder.Services.AddScoped<CreateTicketCommand>();

// Use in endpoints
app.MapGet("/ticket/{id}", async (string id, GetTicketQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync(); // ? Automatic OTEL + Serilog!
});

app.MapPost("/ticket", async (CreateTicketRequest req, CreateTicketCommand cmd) =>
{
    cmd.Title = req.Title;
    return await cmd.ExecuteAsync(); // ? Automatic OTEL + Serilog!
});
```

---

## ?? Comparison

### Before (Standard Approach)

```csharp
public class GetTicketQuery(
    ILogger<GetTicketQuery> logger,              // ? 1
    ActivitySource activitySource,                // ? 2
    SlckEnvelopeObservabilityOptions? options,   // ? 3
    ITicketRepository repo)                       // ? 4
    : ObservableQuery<Ticket>(logger, activitySource, options)
{
    // Need to store repo manually
}
```

**Lines of code**: ~8-10 lines just for constructor

### After (Auto Approach)

```csharp
public class GetTicketQuery(
    IHttpContextAccessor httpContextAccessor,    // ? 1
    ITicketRepository repo)                       // ? 2
    : AutoObservableQuery<Ticket>(httpContextAccessor)
{
    // Need to store repo manually
}
```

**Lines of code**: ~4 lines

**Reduction: 50-60% less boilerplate!**

---

## ?? How It Works

The `Auto*` base classes use service location pattern:

```csharp
public abstract class AutoObservableQuery<TResult>
{
    protected AutoObservableQuery(IHttpContextAccessor httpContextAccessor) { }

    // ? Lazy resolution - only created when accessed
    public ILogger Logger => 
        _httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType());

    public ActivitySource ActivitySource => 
        _httpContext.RequestServices.GetRequiredService<ActivitySource>();

    protected SlckEnvelopeObservabilityOptions? Options => 
        _httpContext.RequestServices.GetRequiredService<SlckEnvelopeObservabilityOptions>();
}
```

Dependencies are resolved from `HttpContext.RequestServices` when first accessed.

---

## ?? When NOT to Use Auto Approach

### Use Standard Approach If:

1. **Unit testing** - You need to mock dependencies explicitly
2. **No HTTP context** - Background jobs, console apps, etc.
3. **Performance critical** - Avoid service location overhead
4. **Explicit dependencies** - You prefer constructor injection clarity

### Example: Unit Testing

```csharp
// ? Hard to test with Auto approach
var query = new GetTicketQuery(mockHttpContextAccessor.Object, tickets);

// ? Easy to test with Standard approach
var query = new GetTicketQuery(
    mockLogger.Object,
    mockActivitySource.Object,
    mockOptions.Object,
    tickets);
```

---

## ? Best Practices

### Development Phase
? Use **Auto approach** for rapid prototyping and quick development

### Production Phase
Consider **Standard approach** if:
- You have extensive unit tests
- Performance is critical
- You prefer explicit DI

### Both Approaches Work Together
```csharp
// Mix and match as needed
builder.Services.AddScoped<SimpleGetTicketQuery>();    // Auto
builder.Services.AddScoped<ComplexCreateCommand>();     // Standard
```

---

## ?? Summary

| Feature | Standard | Auto |
|---------|----------|------|
| **Constructor Parameters** | 3-4+ | 1-2 |
| **Boilerplate** | High | Low |
| **Testability** | Excellent | Good |
| **Performance** | Best | Good |
| **Development Speed** | Slower | Faster |
| **Clarity** | Explicit | Implicit |

**Recommendation**: 
- Start with **Auto** for speed
- Switch to **Standard** for complex scenarios or testing needs

Both approaches give you **automatic OTEL + Serilog** - choose based on your needs! ??
