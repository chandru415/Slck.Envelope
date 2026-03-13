using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Observability;

namespace sample.api.Examples;

/// <summary>
/// Example 1: Query WITHOUT options parameter (SIMPLEST - RECOMMENDED FOR MOST CASES)
/// Observability is ENABLED by default even without injecting options.
/// </summary>
public class BasicGetTicketQuery : ObservableQuery<Ticket>
{
    private readonly List<Ticket> _tickets;

    // ? SIMPLE: Only 3 constructor parameters (no options!)
    public BasicGetTicketQuery(
        ILogger<BasicGetTicketQuery> logger,
        ActivitySource activitySource,
        List<Ticket> tickets)
        : base(logger, activitySource)  // ? No options parameter!
    {
        _tickets = tickets;
    }

    public string TicketId { get; set; } = string.Empty;

    public override async Task<IResult> HandleAsync()
    {
        // ? Automatic OTEL + Serilog!
        // ? Observability ENABLED by default (options = null ? enabled = true)
        // ? No custom tags from appsettings.json (because options is null)
        
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

/// <summary>
/// Example 2: Query WITH options parameter (FOR CONFIGURATION CONTROL)
/// Use this when you need to toggle features or add custom tags from appsettings.json.
/// </summary>
public class ConfigurableGetTicketQuery : ObservableQuery<Ticket>
{
    private readonly List<Ticket> _tickets;

    // ? WITH OPTIONS: 4 constructor parameters (includes options for config control)
    public ConfigurableGetTicketQuery(
        ILogger<ConfigurableGetTicketQuery> logger,
        ActivitySource activitySource,
        List<Ticket> tickets,
        SlckEnvelopeObservabilityOptions options)  // ? Inject options for config
        : base(logger, activitySource, options)
    {
        _tickets = tickets;
    }

    public string TicketId { get; set; } = string.Empty;

    public override async Task<IResult> HandleAsync()
    {
        // ? Automatic OTEL + Serilog!
        // ? Respects configuration from appsettings.json
        // ? Gets custom tags from appsettings.json (Environment, Application, etc.)
        // ? Can be disabled via config without rebuilding
        
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

/*
 * COMPARISON:
 * 
 * BasicGetTicketQuery (WITHOUT options):
 * - Constructor params: 3
 * - Observability: ENABLED by default
 * - Custom tags: NO (options = null)
 * - Config control: NO
 * - Use when: Quick development, simple scenarios
 * 
 * ConfigurableGetTicketQuery (WITH options):
 * - Constructor params: 4
 * - Observability: Controlled by appsettings.json
 * - Custom tags: YES (from appsettings.json)
 * - Config control: YES (toggle without rebuilding)
 * - Use when: Production code, need config control
 * 
 * RECOMMENDATION:
 * - Development/Prototyping: Use BasicGetTicketQuery (no options)
 * - Production: Use ConfigurableGetTicketQuery (with options)
 */
