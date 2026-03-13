using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Slck.Envelope.Decorators;
using Slck.Envelope.Observability;

namespace sample.api.Services;

/// <summary>
/// Example: Normal service using ObservableService base class
/// Automatic OTEL + Serilog with ZERO manual code - respects appsettings.json!
/// </summary>
public class TicketService : ObservableService
{
    private readonly List<Ticket> _tickets;

    public TicketService(
        ILogger<TicketService> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    public async Task<Ticket> CreateTicketAsync(string title)
    {
        // ? Automatic OTEL + Serilog! No manual logging needed.
        // ? Respects configuration from appsettings.json
        return await ExecuteObservableAsync(
            "CreateTicket",
            async () =>
            {
                var ticket = new Ticket
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title
                };

                _tickets.Add(ticket);
                await Task.Delay(10); // Simulate async work
                
                return ticket;
            },
            new Dictionary<string, object>
            {
                ["ticket.title"] = title
            });
    }

    public Ticket? GetTicket(string id)
    {
        // ? Sync version - also automatic OTEL + Serilog
        return ExecuteObservable(
            "GetTicket",
            () => _tickets.FirstOrDefault(t => t.Id == id),
            new Dictionary<string, object>
            {
                ["ticket.id"] = id
            });
    }

    public async Task<List<Ticket>> GetAllTicketsAsync(int page, int pageSize)
    {
        return await ExecuteObservableAsync(
            "GetAllTickets",
            async () =>
            {
                await Task.Delay(5); // Simulate async work
                
                return _tickets
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            },
            new Dictionary<string, object>
            {
                ["page"] = page,
                ["pageSize"] = pageSize,
                ["totalTickets"] = _tickets.Count
            });
    }

    public void DeleteTicket(string id)
    {
        ExecuteObservable(
            "DeleteTicket",
            () =>
            {
                var ticket = _tickets.FirstOrDefault(t => t.Id == id);
                if (ticket != null)
                {
                    _tickets.Remove(ticket);
                }
            },
            new Dictionary<string, object>
            {
                ["ticket.id"] = id
            });
    }
}
