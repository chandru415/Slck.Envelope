using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slck.Envelope.Observability;

namespace Slck.Envelope.MediatR;

/// <summary>
/// Ultra-simplified MediatR handler - ONLY inject IHttpContextAccessor + YOUR dependencies!
/// Logger, ActivitySource, and Options are resolved automatically.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type</typeparam>
/// <typeparam name="TResponse">The response type (typically IResult)</typeparam>
public abstract class AutoObservableRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ILogger? _logger;
    private ActivitySource? _activitySource;
    private SlckEnvelopeObservabilityOptions? _options;

    protected AutoObservableRequestHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ILogger Logger => _logger ??= GetService<ILoggerFactory>().CreateLogger(GetType());
    public ActivitySource ActivitySource => _activitySource ??= GetService<ActivitySource>();
    protected SlckEnvelopeObservabilityOptions? Options => _options ??= GetService<SlckEnvelopeObservabilityOptions>();

    private T GetService<T>() where T : notnull
    {
        var services = _httpContextAccessor?.HttpContext?.RequestServices;
        if (services == null)
            throw new InvalidOperationException("HttpContext not available. Make sure IHttpContextAccessor is registered.");
        
        return services.GetRequiredService<T>();
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
