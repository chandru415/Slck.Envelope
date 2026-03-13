using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Observability;

namespace sample.api.Commands;

/// <summary>
/// Example command - automatically gets OTEL tracing and Serilog logging
/// by inheriting from ObservableCommand. Configuration from appsettings.json applies automatically!
/// </summary>
public class CreateTicketCommand : ObservableCommand<Ticket>
{
    private readonly List<Ticket> _tickets;

    // Constructor: ILogger, ActivitySource, and Options are automatically injected
    public CreateTicketCommand(
        ILogger<CreateTicketCommand> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    public string Title { get; set; } = string.Empty;

    // Just implement your business logic - OTEL & Serilog are automatic!
    public override async Task<IResult> HandleAsync()
    {
        // Logger and ActivitySource are available via protected properties
        Logger.LogInformation("Creating ticket with title: {Title}", Title);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid().ToString(),
            Title = Title
        };

        _tickets.Add(ticket);

        // Add custom tags to the OTEL span
        Activity.Current?.SetTag("ticket.id", ticket.Id);

        Logger.LogInformation("Ticket created successfully with ID: {TicketId}", ticket.Id);

        return Envelope.Created($"/ticket/{ticket.Id}", ticket);
    }
}
