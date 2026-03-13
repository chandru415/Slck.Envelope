using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Slck.Envelope.Observability;

/// <summary>
/// Base class for observable queries with automatic OTEL tracing and Serilog logging.
/// This is OPTIONAL - you can implement IObservableHandler directly for more flexibility.
/// Automatically respects configuration from appsettings.json.
/// </summary>
/// <typeparam name="TResult">The result type returned by the query</typeparam>
public abstract class ObservableQuery<TResult> : IObservableQuery<TResult>, IObservableHandler<IResult>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    protected SlckEnvelopeObservabilityOptions? Options { get; }

    /// <summary>
    /// Initializes a new instance of ObservableQuery.
    /// </summary>
    /// <param name="logger">Logger instance for this query</param>
    /// <param name="activitySource">ActivitySource for OTEL tracing</param>
    /// <param name="options">OPTIONAL: Configuration options. If null, observability is enabled by default.
    /// Inject this parameter ONLY if you need configuration control from appsettings.json.
    /// For most cases, you can omit this parameter.</param>
    protected ObservableQuery(
        ILogger logger,
        ActivitySource activitySource,
        SlckEnvelopeObservabilityOptions? options = null)
    {
        Logger = logger;
        ActivitySource = activitySource;
        Options = options;
    }

    /// <summary>
    /// Executes the query with automatic OTEL tracing and Serilog logging.
    /// </summary>
    public async Task<IResult> ExecuteAsync()
    {
        return await ObservableHandlerExecutor.ExecuteQueryAsync(this, Options);
    }

    /// <summary>
    /// Override this method to implement your query logic.
    /// OTEL tracing and Serilog logging are automatically handled.
    /// </summary>
    public abstract Task<IResult> HandleAsync();
}
