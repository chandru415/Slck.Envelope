using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Slck.Envelope.Observability;

/// <summary>
/// Interface for handlers that require OTEL tracing and Serilog logging infrastructure.
/// Similar to MediatR's IRequestHandler pattern - implement this for automatic observability.
/// </summary>
/// <typeparam name="TResult">The result type</typeparam>
public interface IObservableHandler<TResult>
{
    /// <summary>
    /// Gets the logger instance for this handler.
    /// </summary>
    ILogger Logger { get; }
    
    /// <summary>
    /// Gets the ActivitySource for OTEL tracing.
    /// </summary>
    ActivitySource ActivitySource { get; }
    
    /// <summary>
    /// Implement this method with your business logic.
    /// OTEL tracing and Serilog logging are handled automatically by the executor.
    /// </summary>
    Task<IResult> HandleAsync();
}
