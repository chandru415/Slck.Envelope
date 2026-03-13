# ?? CQRS with Auto Observability - Quick Start

## What You Just Got

? **Automatic OTEL Tracing** - Every command/query creates distributed traces  
? **Automatic Serilog Logging** - All logs enriched with TraceId, SpanId, operation name  
? **Zero Boilerplate** - Just inherit and implement `HandleAsync()`  
? **Type-Safe CQRS** - Separate commands (write) from queries (read)

---

## ?? Usage Pattern

### Step 1: Create a Command or Query Class

**Command (writes data):**
```csharp
public class CreateTicketCommand : ObservableCommand<Ticket>
{
    public CreateTicketCommand(ILogger<CreateTicketCommand> logger, ActivitySource activitySource) 
        : base(logger, activitySource) { }
    
    public string Title { get; set; } = "";

    protected override async Task<IResult> HandleAsync()
    {
        // Your logic here - Logger and ActivitySource are automatically available
        Logger.LogInformation("Creating ticket: {Title}", Title);
        
        return Envelope.Created("/ticket/123", new Ticket { Title = Title });
    }
}
```

**Query (reads data):**
```csharp
public class GetTicketByIdQuery : ObservableQuery<Ticket>
{
    public GetTicketByIdQuery(ILogger<GetTicketByIdQuery> logger, ActivitySource activitySource) 
        : base(logger, activitySource) { }
    
    public string Id { get; set; } = "";

    protected override async Task<IResult> HandleAsync()
    {
        Logger.LogInformation("Fetching ticket: {Id}", Id);
        
        return Envelope.Ok(new Ticket { Id = Id });
    }
}
```

### Step 2: Register in DI

```csharp
builder.Services.AddSlckEnvelopeObservability(); // ? Registers ActivitySource
builder.Services.AddScoped<CreateTicketCommand>();
builder.Services.AddScoped<GetTicketByIdQuery>();
```

### Step 3: Use in Endpoints

```csharp
app.MapPost("/ticket", async (CreateTicketRequest req, CreateTicketCommand cmd) =>
{
    cmd.Title = req.Title;
    return await cmd.ExecuteAsync(); // ? Auto-traced & logged!
});

app.MapGet("/ticket/{id}", async (string id, GetTicketByIdQuery query) =>
{
    query.Id = id;
    return await query.ExecuteAsync(); // ? Auto-traced & logged!
});
```

---

## ?? What Happens Automatically

### OTEL Traces Created
```
Span: Command.CreateTicketCommand
  Tags:
    - command.type: CreateTicketCommand
    - command.category: write
    - TraceId: 8d3c4b2a1f6e5d7c
    - SpanId: a1b2c3d4
```

### Serilog Logs Enriched
```json
{
  "Message": "Creating ticket: New Feature",
  "CommandName": "CreateTicketCommand",
  "TraceId": "8d3c4b2a1f6e5d7c",
  "SpanId": "a1b2c3d4"
}
```

---

## ?? Customize Your Traces & Logs

### Add Custom Tags
```csharp
protected override async Task<IResult> HandleAsync()
{
    Activity.Current?.SetTag("user.id", UserId);
    Activity.Current?.SetTag("ticket.priority", "high");
    
    // Your logic
}
```

### Add Custom Logs
```csharp
protected override async Task<IResult> HandleAsync()
{
    Logger.LogInformation("Processing ticket for user {UserId}", UserId);
    
    // TraceId and SpanId are automatically included!
}
```

---

## ?? View Your Traces

### Local Development (Jaeger)
```bash
docker run -d --name jaeger \
  -p 6831:6831/udp \
  -p 16686:16686 \
  jaegertracing/all-in-one:latest
```

Then add to your app:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource("TicketAPI")
        .AddJaegerExporter());
```

Visit: http://localhost:16686

---

## ?? Complete Example

See `samples/sample.api/` for working examples:
- `Commands/CreateTicketCommand.cs` - Write operation
- `Queries/GetTicketByIdQuery.cs` - Read operation
- `Queries/GetAllTicketsQuery.cs` - Paginated query
- `Program.cs` - Full setup

---

## ?? Pro Tips

1. **Use dependency injection** - Inject repositories, services into your command/query constructors
2. **Keep commands/queries focused** - One responsibility per class
3. **Add validation** - Check inputs in `HandleAsync()` before processing
4. **Use Activity.Current** - Add custom tags for better trace context
5. **Log structured data** - Use `{PropertyName}` syntax for queryable logs

---

## ?? Key Benefits

| Benefit | Description |
|---------|-------------|
| **Observability** | See exactly what's happening in production |
| **Debugging** | Trace requests across microservices |
| **Performance** | Identify slow operations via spans |
| **Consistency** | All commands/queries follow same pattern |
| **Testability** | Easy to unit test - just mock ILogger & ActivitySource |

---

## ?? Learn More

- [Full Observability Documentation](../docs/OBSERVABILITY.md)
- [CQRS Pattern Explained](https://martinfowler.com/bliki/CQRS.html)
- [OpenTelemetry Docs](https://opentelemetry.io/docs/)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Best-Practices)
