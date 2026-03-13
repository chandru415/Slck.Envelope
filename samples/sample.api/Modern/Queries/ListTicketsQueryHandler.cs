using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Contracts;
using Slck.Envelope.Observability;

namespace sample.api.Modern.Queries;

/// <summary>
/// MODERN APPROACH: Pure interface implementation (no base class)
/// Implements IObservableHandler directly for maximum flexibility.
/// Can be combined with other interfaces (IRequestHandler, etc.)
/// </summary>
public class ListTicketsQueryHandler 
    : IObservableHandler<IResult>,
      IObservableQuery<List<Ticket>>
{
    private readonly List<Ticket> _tickets;

    // Required by IObservableHandler interface
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }

    // Query parameters (set by endpoint before execution)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public ListTicketsQueryHandler(
        ILogger<ListTicketsQueryHandler> logger,
        ActivitySource activitySource,
        List<Ticket> tickets)
    {
        Logger = logger;
        ActivitySource = activitySource;
        _tickets = tickets;
    }

    // ExecuteAsync - calls the executor for automatic OTEL + Serilog
    public async Task<IResult> ExecuteAsync()
    {
        return await ObservableHandlerExecutor.ExecuteQueryAsync(this);
    }

    // HandleAsync - implement your business logic
    // Executor wraps this with OTEL span and Serilog scope
    public async Task<IResult> HandleAsync()
    {
        // Add custom OTEL tags
        Activity.Current?.SetTag("query.page", Page);
        Activity.Current?.SetTag("query.pageSize", PageSize);
        Activity.Current?.SetTag("query.totalRecords", _tickets.Count);

        // Logger is available (provided via interface property)
        Logger.LogInformation("Fetching tickets - Page: {Page}, PageSize: {PageSize}", Page, PageSize);

        var total = _tickets.Count;
        var paged = _tickets.Skip((Page - 1) * PageSize).Take(PageSize).ToList();

        var meta = new PaginationMeta
        {
            Page = Page,
            PageSize = PageSize,
            Total = total
        };

        Activity.Current?.SetTag("query.resultCount", paged.Count);
        Activity.Current?.SetTag("query.hasMore", meta.HasMore);

        Logger.LogInformation("Retrieved {Count} tickets out of {Total}", paged.Count, total);

        return await Task.FromResult(Envelope.Ok(paged, meta));
    }
}
