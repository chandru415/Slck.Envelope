using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.MediatR;
using Slck.Envelope.Observability;
using System.Diagnostics;

namespace sample.api.MediatR.Queries;

/// <summary>
/// MediatR request
/// </summary>
public record GetTicketByIdRequest(string Id) : IRequest<IResult>;

/// <summary>
/// MediatR handler with automatic OTEL + Serilog using Slck.Envelope.MediatR
/// </summary>
public class GetTicketByIdRequestHandler : ObservableRequestHandler<GetTicketByIdRequest, IResult>
{
    private readonly List<Ticket> _tickets;

    public GetTicketByIdRequestHandler(
        ILogger<GetTicketByIdRequestHandler> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    protected override async Task<IResult> HandleAsync(GetTicketByIdRequest request, CancellationToken cancellationToken)
    {
        // ? Automatic OTEL + Serilog!
        // ? Logger available
        // ? ActivitySource available
        // ? Just write business logic!

        Logger.LogInformation("Fetching ticket: {TicketId}", request.Id);

        var ticket = _tickets.FirstOrDefault(t => t.Id == request.Id);

        if (ticket is null)
        {
            Logger.LogWarning("Ticket not found: {TicketId}", request.Id);
            return Envelope.NotFound($"Ticket '{request.Id}' not found");
        }

        Logger.LogInformation("Ticket found: {TicketId}", request.Id);
        return Envelope.Ok(ticket);
    }
}
