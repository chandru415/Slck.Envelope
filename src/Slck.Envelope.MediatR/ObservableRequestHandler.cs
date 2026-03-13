using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.Observability;

namespace Slck.Envelope.MediatR;

/// <summary>
/// Base class that combines MediatR IRequestHandler with Slck.Envelope observability.
/// Automatically provides OTEL tracing and Serilog logging for all MediatR requests.
/// Respects configuration from appsettings.json.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type</typeparam>
/// <typeparam name="TResponse">The response type (typically IResult)</typeparam>
public abstract class ObservableRequestHandler<TRequest, TResponse> 
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ILogger Logger { get; }
    public ActivitySource ActivitySource { get; }
    protected SlckEnvelopeObservabilityOptions? Options { get; }

    protected ObservableRequestHandler(
        ILogger logger, 
        ActivitySource activitySource,
        SlckEnvelopeObservabilityOptions? options = null)
    {
        Logger = logger;
        ActivitySource = activitySource;
        Options = options;
    }

    /// <summary>
    /// MediatR entry point - automatically wrapped with OTEL + Serilog
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var enabled = Options?.Enabled ?? true;
        var enableTracing = Options?.EnableAutoTracing ?? true;
        var enableLogging = Options?.EnableSerilogEnrichment ?? true;

        if (!enabled)
        {
            return await HandleAsync(request, cancellationToken);
        }

        var handlerName = GetType().Name;
        var requestName = typeof(TRequest).Name;
        
        // Start OTEL activity
        Activity? activity = null;
        if (enableTracing)
        {
            activity = ActivitySource.StartActivity($"MediatR.{handlerName}", ActivityKind.Internal);
            activity?.SetTag("mediatr.request", requestName);
            activity?.SetTag("mediatr.handler", handlerName);

            // Add default tags from configuration
            if (Options?.DefaultTags != null)
            {
                foreach (var tag in Options.DefaultTags)
                {
                    activity?.SetTag(tag.Key, tag.Value);
                }
            }
        }

        try
        {
            TResponse response;
            if (enableLogging)
            {
                // Create Serilog scope
                using (Logger.BeginScope(new Dictionary<string, object>
                {
                    ["RequestName"] = requestName,
                    ["HandlerName"] = handlerName,
                    ["TraceId"] = activity?.TraceId.ToString() ?? "none",
                    ["SpanId"] = activity?.SpanId.ToString() ?? "none"
                }))
                {
                    Logger.LogInformation("Handling MediatR request: {RequestName}", requestName);
                    response = await HandleAsync(request, cancellationToken);
                    Logger.LogInformation("MediatR request {RequestName} completed successfully", requestName);
                }
            }
            else
            {
                response = await HandleAsync(request, cancellationToken);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MediatR request {RequestName} failed: {ErrorMessage}", requestName, ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <summary>
    /// Implement this method with your business logic.
    /// OTEL tracing and Serilog logging are handled automatically.
    /// </summary>
    protected abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
