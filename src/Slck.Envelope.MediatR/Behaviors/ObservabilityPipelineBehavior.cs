using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slck.Envelope.Observability;

namespace Slck.Envelope.MediatR.Behaviors;

/// <summary>
/// MediatR pipeline behavior that adds OTEL tracing and Serilog logging to ALL requests.
/// Register this to automatically instrument every MediatR request in your app.
/// Respects configuration from appsettings.json.
/// </summary>
public class ObservabilityPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ObservabilityPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly ActivitySource _activitySource;
    private readonly SlckEnvelopeObservabilityOptions _options;

    public ObservabilityPipelineBehavior(
        ILogger<ObservabilityPipelineBehavior<TRequest, TResponse>> logger,
        ActivitySource activitySource,
        IOptions<SlckEnvelopeObservabilityOptions> options)
    {
        _logger = logger;
        _activitySource = activitySource;
        _options = options.Value;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var enabled = _options.Enabled;
        var enableTracing = _options.EnableAutoTracing;
        var enableLogging = _options.EnableSerilogEnrichment;

        if (!enabled)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        
        // Create OTEL span for the entire MediatR pipeline
        Activity? activity = null;
        if (enableTracing)
        {
            activity = _activitySource.StartActivity($"MediatR.Pipeline.{requestName}", ActivityKind.Internal);
            activity?.SetTag("mediatr.request.type", requestName);
            activity?.SetTag("mediatr.pipeline", "observability");

            // Add default tags from configuration
            if (_options.DefaultTags != null)
            {
                foreach (var tag in _options.DefaultTags)
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
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["MediatRRequest"] = requestName,
                    ["TraceId"] = activity?.TraceId.ToString() ?? "none",
                    ["SpanId"] = activity?.SpanId.ToString() ?? "none"
                }))
                {
                    _logger.LogInformation("MediatR Pipeline: Processing {RequestName}", requestName);
                    response = await next();
                    _logger.LogInformation("MediatR Pipeline: {RequestName} completed", requestName);
                }
            }
            else
            {
                response = await next();
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MediatR Pipeline: {RequestName} failed", requestName);
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
}
