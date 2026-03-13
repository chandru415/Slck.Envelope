using Microsoft.AspNetCore.Http;

namespace Slck.Envelope.Observability;

/// <summary>
/// Marker interface for commands that require automatic OTEL tracing and Serilog logging.
/// Implement this interface to enable automatic instrumentation for write operations.
/// </summary>
/// <typeparam name="TResult">The result type returned by the command</typeparam>
public interface IObservableCommand<TResult>
{
    /// <summary>
    /// Executes the command with automatic OTEL tracing and Serilog logging.
    /// </summary>
    Task<IResult> ExecuteAsync();
}
