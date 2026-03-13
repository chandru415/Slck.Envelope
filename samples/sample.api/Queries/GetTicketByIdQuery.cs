using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Observability;

namespace sample.api.Queries;

/// <summary>
/// Example query - automatically gets OTEL tracing and Serilog logging
/// by inheriting from ObservableQuery. Configuration from appsettings.json applies automatically!
/// </summary>
public class GetTicketByIdQuery : ObservableQuery<Ticket>
{
    private readonly List<Ticket> _tickets;

    // Constructor: ILogger, ActivitySource, and Options are automatically injected
    public GetTicketByIdQuery(
        ILogger<GetTicketByIdQuery> logger, 
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null) 
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    public string TicketId { get; set; } = string.Empty;

    // Just implement your business logic - OTEL & Serilog are automatic!
    public override async Task<IResult> HandleAsync()
    {
        // Logger and ActivitySource are available via protected properties
        Logger.LogInformation("Searching for ticket with ID: {TicketId}", TicketId);

        var ticket = _tickets.FirstOrDefault(t => t.Id == TicketId);

        if (ticket is null)
        {
            Logger.LogWarning("Ticket not found: {TicketId}", TicketId);
            return Envelope.NotFound($"Ticket '{TicketId}' not found");
        }

        // Add custom tags to the OTEL span
        Activity.Current?.SetTag("ticket.title", ticket.Title);

        Logger.LogInformation("Ticket found: {TicketId}", TicketId);

        return Envelope.Ok(ticket);
    }
}
