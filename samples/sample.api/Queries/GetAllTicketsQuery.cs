using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.AspNetCore;
using Slck.Envelope.Contracts;
using Slck.Envelope.Observability;

namespace sample.api.Queries;

/// <summary>
/// Example: Query with pagination - shows how to use meta information
/// </summary>
public class GetAllTicketsQuery : ObservableQuery<List<Ticket>>
{
    private readonly List<Ticket> _tickets;

    public GetAllTicketsQuery(
        ILogger<GetAllTicketsQuery> logger,
        ActivitySource activitySource,
        List<Ticket> tickets)
        : base(logger, activitySource)
    {
        _tickets = tickets;
    }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public override async Task<IResult> HandleAsync()
    {
        // Add custom OTEL tags for the query parameters
        Activity.Current?.SetTag("query.page", Page);
        Activity.Current?.SetTag("query.pageSize", PageSize);
        Activity.Current?.SetTag("query.totalRecords", _tickets.Count);

        Logger.LogInformation("Fetching tickets - Page: {Page}, PageSize: {PageSize}", Page, PageSize);

        var total = _tickets.Count;
        var paged = _tickets.Skip((Page - 1) * PageSize).Take(PageSize).ToList();

        var meta = new PaginationMeta
        {
            Page = Page,
            PageSize = PageSize,
            Total = total
        };

        // Add result metrics to trace
        Activity.Current?.SetTag("query.resultCount", paged.Count);
        Activity.Current?.SetTag("query.hasMore", meta.HasMore);

        Logger.LogInformation("Retrieved {Count} tickets out of {Total}", paged.Count, total);

        return await Task.FromResult(Envelope.Ok(paged, meta));
    }
}
