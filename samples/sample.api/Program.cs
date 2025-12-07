using sample.api;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseApiEnvelope();



// In-memory collection for demo
var tickets = new List<Ticket>
{
     new() { Id = "123", Title = "Sample Ticket A" },
    new() { Id = "124", Title = "Sample Ticket B" },
    new() { Id = "125", Title = "Sample Ticket C" }
};

// Example endpoints
app.MapGet("/ticket/{id}", (string id) =>
{
    var ticket = tickets.FirstOrDefault(t => t.Id == id);
    return ticket is null
        ? Envelope.NotFound($"Ticket '{id}' not found")
        : Envelope.Ok(ticket);
});

app.MapGet("/boom", () =>
{
    // This will be caught by middleware in production
    throw new InvalidOperationException("Something went wrong!");
});

// GET all tickets with pagination metadata
app.MapGet("/tickets", (int page = 1, int pageSize = 10) =>
{
    var total = tickets.Count;
    var paged = tickets.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    var meta = new PaginationMeta
    {
        Page = page,
        PageSize = pageSize,
        Total = total
    };

    return Envelope.Ok(paged, meta);
});

app.Run();
