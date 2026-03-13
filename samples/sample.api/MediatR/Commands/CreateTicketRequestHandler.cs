using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.MediatR;
using Slck.Envelope.Observability;
using System.Diagnostics;

namespace sample.api.MediatR.Commands;

/// <summary>
/// MediatR request
/// </summary>
public record CreateTicketRequest(string Title) : IRequest<IResult>;

/// <summary>
/// MediatR handler with automatic OTEL + Serilog using Slck.Envelope.MediatR
/// </summary>
public class CreateTicketRequestHandler : ObservableRequestHandler<CreateTicketRequest, IResult>
{
    private readonly List<Ticket> _tickets;

    public CreateTicketRequestHandler(
        ILogger<CreateTicketRequestHandler> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    protected override async Task<IResult> HandleAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        // ? Automatic OTEL + Serilog!
        Logger.LogInformation("Creating ticket: {Title}", request.Title);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title
        };

        _tickets.Add(ticket);

        Logger.LogInformation("Ticket created: {TicketId}", ticket.Id);
        return Envelope.Created($"/ticket/{ticket.Id}", ticket);
    }
}
