using Microsoft.AspNetCore.Http;

namespace Slck.Envelope.Observability;

/// <summary>
/// Marker interface for queries that require automatic OTEL tracing and Serilog logging.
/// Any class implementing this interface gets auto-instrumentation when executed.
/// </summary>
/// <typeparam name="TResult">The result type returned by the query</typeparam>
public interface IObservableQuery<TResult>
{
    Task<IResult> ExecuteAsync();
}
