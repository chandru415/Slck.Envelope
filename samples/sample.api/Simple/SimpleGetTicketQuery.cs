using Microsoft.AspNetCore.Http;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Observability;

namespace sample.api.Simple;

/// <summary>
/// ULTRA SIMPLE - Only inject IHttpContextAccessor and your own dependencies!
/// Logger, ActivitySource, and Options are resolved automatically.
/// </summary>
public class SimpleGetTicketQuery : AutoObservableQuery<Ticket>
{
    private readonly List<Ticket> _tickets;

    // ? ONLY inject what YOU need + IHttpContextAccessor
    // ? NO ILogger, NO ActivitySource, NO Options!
    public SimpleGetTicketQuery(
        IHttpContextAccessor httpContextAccessor,
        List<Ticket> tickets) 
        : base(httpContextAccessor)
    {
        _tickets = tickets;
    }

    public string TicketId { get; set; } = string.Empty;

    public override async Task<IResult> HandleAsync()
    {
        // ? Logger is available automatically!
        // ? ActivitySource is available automatically!
        // ? Options is available automatically!
        
        Logger.LogInformation("Searching for ticket: {TicketId}", TicketId);

        var ticket = _tickets.FirstOrDefault(t => t.Id == TicketId);

        if (ticket is null)
        {
            Logger.LogWarning("Ticket not found: {TicketId}", TicketId);
            return Envelope.NotFound($"Ticket '{TicketId}' not found");
        }

        Logger.LogInformation("Ticket found: {TicketId}", TicketId);

        return Envelope.Ok(ticket);
    }
}
