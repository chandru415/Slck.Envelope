using sample.api;
using sample.api.Commands;
using sample.api.Modern.Queries;
using sample.api.Queries;
using sample.api.Services;
using sample.api.Simple;
using Slck.Envelope.Observability;
using Slck.Envelope.MediatR;
using MediatR;
using sample.api.MediatR.Commands;
using sample.api.MediatR.Queries;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ================================================================================
// ? ZERO-CONFIGURATION OBSERVABILITY!
// ================================================================================

// ? ONE LINE - Automatic Serilog + OpenTelemetry registration!
// No manual UseSerilog(), no manual AddOpenTelemetry() - just configuration!
builder.Services.AddSlckEnvelopeObservability(builder.Configuration);

// ? MediatR integration (also automatic!)
builder.Services.AddSlckEnvelopeMediatR(
    builder.Configuration,
    cfg =>
    {
        cfg.RegisterServicesFromAssemblies(
            typeof(Program).Assembly,
            Assembly.GetExecutingAssembly());
    });

// ================================================================================
// Register in-memory ticket collection as singleton
// =============================================================================
var tickets = new List<Ticket>
{
    new() { Id = "123", Title = "Sample Ticket A" },
    new() { Id = "124", Title = "Sample Ticket B" },
    new() { Id = "125", Title = "Sample Ticket C" }
};
builder.Services.AddSingleton(tickets);

// ================================================================================
// Register handlers for different approaches
// ================================================================================

// Package 1: Slck.Envelope - CQRS handlers
builder.Services.AddScoped<CreateTicketCommand>();
builder.Services.AddScoped<GetTicketByIdQuery>();
builder.Services.AddScoped<GetAllTicketsQuery>();
builder.Services.AddScoped<ListTicketsQueryHandler>();
builder.Services.AddScoped<SimpleCreateTicketCommand>();
builder.Services.AddScoped<SimpleGetTicketQuery>();

// Package 3: Slck.Envelope.Decorators - Observable services
builder.Services.AddScoped<TicketService>();

// Package 2: MediatR handlers are auto-registered by AddSlckEnvelopeMediatR

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ? Use Serilog request logging (automatic)
app.UseSlckEnvelopeSerilog();

// ? Use Slck.Envelope middleware
app.UseSlckEnvelope();

// ================================================================================
// ? PACKAGE 1: Slck.Envelope (CQRS) - Automatic OTEL + Serilog
// ================================================================================

app.MapGet("/cqrs/ticket/{id}", async (string id, GetTicketByIdQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync(); // ? Automatic OTEL + Serilog!
})
.WithName("CQRS_GetTicket")
.WithTags("Package1-CQRS")
.WithSummary("Uses Slck.Envelope base class with automatic observability");

app.MapPost("/cqrs/ticket", async (CreateTicketRequest request, CreateTicketCommand command) =>
{
    command.Title = request.Title;
    return await command.ExecuteAsync(); // ? Automatic OTEL + Serilog!
})
.WithName("CQRS_CreateTicket")
.WithTags("Package1-CQRS");

app.MapGet("/cqrs/tickets", async (int page, int pageSize, ListTicketsQueryHandler handler) =>
{
    handler.Page = page;
    handler.PageSize = pageSize;
    return await handler.ExecuteAsync(); // ? Automatic OTEL + Serilog!
})
.WithName("CQRS_ListTickets")
.WithTags("Package1-CQRS");

// ================================================================================
// ? PACKAGE 2: Slck.Envelope.MediatR - Automatic OTEL + Serilog for MediatR
// ================================================================================

app.MapGet("/mediatr/ticket/{id}", async (string id, IMediator mediator) =>
{
    return await mediator.Send(new GetTicketByIdRequest(id)); // ? Automatic OTEL + Serilog!
})
.WithName("MediatR_GetTicket")
.WithTags("Package2-MediatR")
.WithSummary("Uses MediatR with automatic observability via pipeline behavior");

app.MapPost("/mediatr/ticket", async (CreateTicketRequest request, IMediator mediator) =>
{
    return await mediator.Send(request); // ? Automatic OTEL + Serilog!
})
.WithName("MediatR_CreateTicket")
.WithTags("Package2-MediatR");

// ================================================================================
// ? PACKAGE 3: Slck.Envelope.Decorators - Wrap any service method
// ================================================================================

app.MapGet("/service/ticket/{id}", async (string id, TicketService service) =>
{
    var ticket = service.GetTicket(id); // ? Automatic OTEL + Serilog!
    return ticket != null 
        ? Results.Ok(ticket)
        : Results.NotFound($"Ticket '{id}' not found");
})
.WithName("Service_GetTicket")
.WithTags("Package3-Decorators")
.WithSummary("Uses ObservableService to wrap any existing service method");

app.MapPost("/service/ticket", async (CreateTicketRequest request, TicketService service) =>
{
    var ticket = await service.CreateTicketAsync(request.Title); // ? Automatic OTEL + Serilog!
    return Results.Created($"/service/ticket/{ticket.Id}", ticket);
})
.WithName("Service_CreateTicket")
.WithTags("Package3-Decorators");

app.MapGet("/service/tickets", async (int page, int pageSize, TicketService service) =>
{
    var tickets = await service.GetAllTicketsAsync(page, pageSize); // ? Automatic OTEL + Serilog!
    return Results.Ok(tickets);
})
.WithName("Service_ListTickets")
.WithTags("Package3-Decorators");

// ================================================================================
// Simple approach endpoints (Auto DI)
// ================================================================================

app.MapGet("/simple/ticket/{id}", async (string id, SimpleGetTicketQuery query) =>
{
    query.TicketId = id;
    return await query.ExecuteAsync();
})
.WithName("Simple_GetTicket")
.WithTags("Simple")
.WithSummary("Uses Auto approach with IHttpContextAccessor");

app.MapPost("/simple/ticket", async (CreateTicketRequest request, SimpleCreateTicketCommand command) =>
{
    command.Title = request.Title;
    return await command.ExecuteAsync();
})
.WithName("Simple_CreateTicket")
.WithTags("Simple");


// Error endpoint for testing
app.MapGet("/boom", () =>
{
    throw new InvalidOperationException("Something went wrong!");
})
.WithName("TestError")
.WithTags("Testing");

app.Run();

// DTO for POST requests
public record CreateTicketRequest(string Title);
