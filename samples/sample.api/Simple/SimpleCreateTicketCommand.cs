using Microsoft.AspNetCore.Http;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Observability;

namespace sample.api.Simple;

/// <summary>
/// ULTRA SIMPLE - Only inject IHttpContextAccessor and your own dependencies!
/// </summary>
public class SimpleCreateTicketCommand : AutoObservableCommand<Ticket>
{
    private readonly List<Ticket> _tickets;

    // ? ONLY inject what YOU need + IHttpContextAccessor
    public SimpleCreateTicketCommand(
        IHttpContextAccessor httpContextAccessor,
        List<Ticket> tickets) 
        : base(httpContextAccessor)
    {
        _tickets = tickets;
    }

    public string Title { get; set; } = string.Empty;

    public override async Task<IResult> HandleAsync()
    {
        // ? Logger, ActivitySource, Options all available automatically!
        
        Logger.LogInformation("Creating ticket: {Title}", Title);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid().ToString(),
            Title = Title
        };

        _tickets.Add(ticket);

        Logger.LogInformation("Ticket created: {TicketId}", ticket.Id);

        return Envelope.Created($"/ticket/{ticket.Id}", ticket);
    }
}
