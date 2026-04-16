using sample.api;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Configure envelope options.
// MapException<T> translates known exception types into specific status codes — no try/catch in handlers needed.
builder.Services.AddApiEnvelope(opts =>
{
    opts.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    opts.CorrelationIdHeader = "X-Correlation-ID";

    opts.MapException<KeyNotFoundException>(404, "not_found")
        .MapException<UnauthorizedAccessException>(401, "unauthorized")
        .MapException<InvalidOperationException>(422, "validation_error", "The operation is invalid");

    // OnBeforeWriteError: attach Activity.Current.TraceId for OpenTelemetry correlation
    opts.OnBeforeWriteError = (ex, response) =>
        response with { RequestId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? response.RequestId };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// Global unhandled-exception handler — must be registered before endpoints.
app.UseApiEnvelope();

// In-memory collection for demo
var tickets = new List<Ticket>
{
    new() { Id = "123", Title = "Sample Ticket A" },
    new() { Id = "124", Title = "Sample Ticket B" },
    new() { Id = "125", Title = "Sample Ticket C" }
};

// GET single ticket
app.MapGet("/ticket/{id}", (string id) =>
{
    var ticket = tickets.FirstOrDefault(t => t.Id == id);
    return ticket is null
        ? Envelope.NotFound($"Ticket '{id}' not found")
        : Envelope.Ok(ticket);
});

// POST create ticket — demonstrates 201 Created with Location header
app.MapPost("/ticket", (Ticket ticket) =>
{
    tickets.Add(ticket);
    return Envelope.Created($"/ticket/{ticket.Id}", ticket);
});

// POST enqueue — demonstrates 202 Accepted with a status-poll Location header
app.MapPost("/ticket/enqueue", (Ticket ticket) =>
{
    tickets.Add(ticket);
    return Envelope.Accepted(new { ticket.Id, Status = "queued" }, $"/ticket/{ticket.Id}/status");
});

// GET status — demonstrates ToResult() extension: service layer returns ApiResponse<T>,
// handler converts it to IResult without knowing about HTTP directly
app.MapGet("/ticket/{id}/status", (string id) =>
{
    var ticket = tickets.FirstOrDefault(t => t.Id == id);
    return ticket is null
        ? Slck.Envelope.Factory.EnvelopeFactory.Fail<object>("not_found", $"Ticket '{id}' not found").ToNotFoundResult()
        : Slck.Envelope.Factory.EnvelopeFactory.Ok(new { ticket.Id, Status = "completed" }).ToOkResult();
});

// GET all tickets with pagination metadata
app.MapGet("/tickets", (int page = 1, int pageSize = 10) =>
{
    var total = tickets.Count;
    var paged = tickets.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    var meta = new PaginationMeta { Page = page, PageSize = pageSize, Total = total };
    return Envelope.Ok(paged, meta);
});

// POST validate — demonstrates 422 UnprocessableEntity with field-level errors
app.MapPost("/ticket/validate", (Ticket ticket) =>
{
    var errors = new Dictionary<string, string[]?>();
    if (string.IsNullOrWhiteSpace(ticket.Id))    errors["id"]    = ["Id is required."];
    if (string.IsNullOrWhiteSpace(ticket.Title)) errors["title"] = ["Title is required."];

    return errors.Count > 0
        ? Envelope.UnprocessableEntity(errors)
        : Envelope.Ok(ticket);
});

// GET boom — mapped exception: InvalidOperationException → 422 (not 500)
// No try/catch needed; the middleware resolves it via ExceptionMap.
app.MapGet("/boom", () =>
{
    throw new InvalidOperationException("Business rule violated");
});

// GET auth-error — mapped exception: UnauthorizedAccessException → 401
app.MapGet("/restricted", () =>
{
    throw new UnauthorizedAccessException();
});

// GET skip — endpoint opted out; its exceptions propagate normally (no envelope 500)
app.MapGet("/raw", () =>
{
    throw new Exception("This won't be wrapped in an envelope");
}).WithMetadata(new SkipEnvelopeAttribute());

// GET escape-hatch — EnvelopeFactory.Fail + Envelope.From for custom status codes
app.MapGet("/custom", () =>
{
    var response = Slck.Envelope.Factory.EnvelopeFactory.Fail<object>("teapot", "I'm a teapot", requestId: "demo-123");
    return Envelope.From(response, 418);
});

app.Run();


