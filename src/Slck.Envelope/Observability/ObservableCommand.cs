using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Slck.Envelope.Observability;

/// <summary>
/// Base class for observable commands with automatic OTEL tracing and Serilog logging.
/// This is OPTIONAL - you can implement IObservableHandler directly for more flexibility.
/// Automatically respects configuration from appsettings.json.
/// </summary>
/// <typeparam name="TResult">The result type returned by the command</typeparam>
public abstract class ObservableCommand<TResult> : IObservableCommand<TResult>, IObservableHandler<IResult>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    protected SlckEnvelopeObservabilityOptions? Options { get; }

    /// <summary>
    /// Initializes a new instance of ObservableCommand.
    /// </summary>
    /// <param name="logger">Logger instance for this command</param>
    /// <param name="activitySource">ActivitySource for OTEL tracing</param>
    /// <param name="options">OPTIONAL: Configuration options. If null, will be auto-resolved from DI if available.
    /// For most cases, you can omit this parameter and it will be resolved automatically.</param>
    protected ObservableCommand(
        ILogger logger,
        ActivitySource activitySource,
        SlckEnvelopeObservabilityOptions? options = null)
    {
        Logger = logger;
        ActivitySource = activitySource;
        Options = options;
    }

    /// <summary>
    /// Executes the command with automatic OTEL tracing and Serilog logging.
    /// </summary>
    public async Task<IResult> ExecuteAsync()
    {
        return await ObservableHandlerExecutor.ExecuteCommandAsync(this, Options);
    }

    /// <summary>
    /// Override this method to implement your command logic.
    /// OTEL tracing and Serilog logging are automatically handled.
    /// </summary>
    public abstract Task<IResult> HandleAsync();
}
