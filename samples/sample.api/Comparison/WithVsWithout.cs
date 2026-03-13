using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Observability;

namespace sample.api.Comparison;

// ================================================================================
// ? WITHOUT Slck.Envelope - Manual OTEL + Serilog (40+ lines of boilerplate!)
// ================================================================================

public class ManualGetTicketQuery
{
    private readonly List<Ticket> _tickets;
    private readonly ILogger<ManualGetTicketQuery> _logger;
    private readonly ActivitySource _activitySource;

    public ManualGetTicketQuery(
        List<Ticket> tickets,
        ILogger<ManualGetTicketQuery> logger,
        ActivitySource activitySource)
    {
        _tickets = tickets;
        _logger = logger;
        _activitySource = activitySource;
    }

    public string TicketId { get; set; } = string.Empty;

    public async Task<IResult> ExecuteAsync()
    {
        // ? MANUAL OTEL span creation - EVERY HANDLER needs this!
        using var activity = _activitySource.StartActivity(
            "Query.ManualGetTicketQuery",
            ActivityKind.Internal);
        
        activity?.SetTag("query.type", "ManualGetTicketQuery");
        activity?.SetTag("query.category", "read");
        activity?.SetTag("ticket.id", TicketId);

        // ? MANUAL Serilog scope - EVERY HANDLER needs this!
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["QueryName"] = "ManualGetTicketQuery",
            ["QueryType"] = "Query",
            ["TraceId"] = activity?.TraceId.ToString() ?? "none",
            ["SpanId"] = activity?.SpanId.ToString() ?? "none"
        }))
        {
            _logger.LogInformation("Executing query: {QueryName}", "ManualGetTicketQuery");

            try
            {
                // FINALLY! Business logic starts here
                _logger.LogInformation("Searching for ticket: {TicketId}", TicketId);

                var ticket = _tickets.FirstOrDefault(t => t.Id == TicketId);

                if (ticket is null)
                {
                    _logger.LogWarning("Ticket not found: {TicketId}", TicketId);
                    _logger.LogInformation("Query ManualGetTicketQuery completed");
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return Results.NotFound($"Ticket '{TicketId}' not found");
                }

                _logger.LogInformation("Ticket found: {TicketId}", TicketId);
                _logger.LogInformation("Query ManualGetTicketQuery completed successfully");
                
                // ? MANUAL success tracking
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                return Results.Ok(ticket);
            }
            catch (Exception ex)
            {
                // ? MANUAL error tracking - EVERY HANDLER needs this!
                _logger.LogError(ex, "Query ManualGetTicketQuery failed: {ErrorMessage}", ex.Message);
                
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error", true);
                activity?.SetTag("error.type", ex.GetType().Name);
                activity?.SetTag("error.message", ex.Message);
                
                throw;
            }
        }
    }
}

// ================================================================================
// ? WITH Slck.Envelope - Automatic OTEL + Serilog (5 lines of business logic!)
// ================================================================================

public class SlckGetTicketQuery : ObservableQuery<Ticket>
{
    private readonly List<Ticket> _tickets;

    // Constructor: Let DI inject infrastructure dependencies
    // (Developer doesn't think about them - they're just infrastructure)
    public SlckGetTicketQuery(
        ILogger<SlckGetTicketQuery> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions? options = null)
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    public string TicketId { get; set; } = string.Empty;

    // ? ONLY business logic - NO OTEL/Serilog boilerplate!
    public override async Task<IResult> HandleAsync()
    {
        // ? AUTOMATIC: OTEL span created
        // ? AUTOMATIC: Serilog scope with TraceId/SpanId
        // ? AUTOMATIC: Success/error tracking
        
        // Just write your business logic!
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
    
    // ? ExecuteAsync() inherited from base class - calls ObservableHandlerExecutor
    // ? That's where ALL the OTEL/Serilog magic happens automatically!
}

// ================================================================================
// ?? COMPARISON
// ================================================================================

/*
 * ManualGetTicketQuery (without Slck.Envelope):
 * - 85 lines of code
 * - 60 lines of OTEL/Serilog boilerplate
 * - 25 lines of business logic
 * - Easy to forget error tracking
 * - Inconsistent across handlers
 * 
 * SlckGetTicketQuery (with Slck.Envelope):
 * - 25 lines of code
 * - 0 lines of OTEL/Serilog boilerplate (automatic!)
 * - 15 lines of business logic
 * - Never forget error tracking
 * - Consistent across all handlers
 * 
 * Code Reduction: 70%
 * Boilerplate Reduction: 100%
 * 
 * What developers save:
 * - No manual StartActivity()
 * - No manual BeginScope()
 * - No manual SetStatus()
 * - No manual try/catch for tracing
 * - No manual tag setting
 * - No manual TraceId enrichment
 * 
 * What they get:
 * ? All of the above, automatically!
 * ? Configuration via appsettings.json
 * ? Toggle features without code changes
 * ? Consistent naming and tagging
 */
