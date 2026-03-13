using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Slck.Envelope.Observability;

/// <summary>
/// Executor that wraps IObservableHandler execution with automatic OTEL tracing and Serilog logging.
/// This is the core engine that provides observability - handlers don't need to inherit from a base class.
/// Respects configuration from appsettings.json.
/// </summary>
public static class ObservableHandlerExecutor
{
    // ? OPTIMIZATION: Cache handler type names to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, string> TypeNameCache = new();

    /// <summary>
    /// Executes a command handler with automatic OTEL tracing and Serilog logging.
    /// </summary>
    public static async Task<IResult> ExecuteCommandAsync<THandler>(
        THandler handler,
        SlckEnvelopeObservabilityOptions? options = null)
        where THandler : IObservableHandler<IResult>
    {
        return await ExecuteAsync(handler, "Command", "write", options);
    }

    /// <summary>
    /// Executes a query handler with automatic OTEL tracing and Serilog logging.
    /// </summary>
    public static async Task<IResult> ExecuteQueryAsync<THandler>(
        THandler handler,
        SlckEnvelopeObservabilityOptions? options = null)
        where THandler : IObservableHandler<IResult>
    {
        return await ExecuteAsync(handler, "Query", "read", options);
    }

    private static async Task<IResult> ExecuteAsync<THandler>(
        THandler handler,
        string handlerType,
        string category,
        SlckEnvelopeObservabilityOptions? options = null)
        where THandler : IObservableHandler<IResult>
    {
        // Use options if provided, otherwise assume enabled
        var enabled = options?.Enabled ?? true;
        var enableTracing = options?.EnableAutoTracing ?? true;
        var enableLogging = options?.EnableSerilogEnrichment ?? true;

        // ? OPTIMIZATION: Use cached type name instead of repeated reflection
        var handlerName = TypeNameCache.GetOrAdd(typeof(THandler), t => t.Name);
        
        // Start OTEL activity (span) only if enabled
        Activity? activity = null;
        if (enabled && enableTracing)
        {
            activity = handler.ActivitySource.StartActivity(
                $"{handlerType}.{handlerName}", 
                ActivityKind.Internal);
            
            activity?.SetTag($"{handlerType.ToLowerInvariant()}.type", handlerName);
            activity?.SetTag($"{handlerType.ToLowerInvariant()}.category", category);

            // Add default tags from configuration
            if (options?.DefaultTags != null)
            {
                foreach (var tag in options.DefaultTags)
                {
                    activity?.SetTag(tag.Key, tag.Value);
                }
            }
        }

        try
        {
            // Create Serilog logging scope with enrichment only if enabled
            if (enabled && enableLogging)
            {
                // ? OPTIMIZATION: Reuse dictionary for scope (avoid allocation if disabled)
                using (handler.Logger.BeginScope(new Dictionary<string, object>
                {
                    [$"{handlerType}Name"] = handlerName,
                    [$"{handlerType}Type"] = handlerType,
                    ["TraceId"] = activity?.TraceId.ToString() ?? "none",
                    ["SpanId"] = activity?.SpanId.ToString() ?? "none"
                }))
                {
                    return await ExecuteHandlerAsync(handler, handlerType, handlerName, activity);
                }
            }
            else
            {
                return await ExecuteHandlerAsync(handler, handlerType, handlerName, activity);
            }
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private static async Task<IResult> ExecuteHandlerAsync<THandler>(
        THandler handler,
        string handlerType,
        string handlerName,
        Activity? activity)
        where THandler : IObservableHandler<IResult>
    {
        handler.Logger.LogInformation("Executing {HandlerType}: {HandlerName}", handlerType.ToLowerInvariant(), handlerName);

        try
        {
            var result = await handler.HandleAsync();
            
            handler.Logger.LogInformation("{HandlerType} {HandlerName} completed successfully", handlerType, handlerName);
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            return result;
        }
        catch (Exception ex)
        {
            handler.Logger.LogError(ex, "{HandlerType} {HandlerName} failed with error: {ErrorMessage}", 
                handlerType, handlerName, ex.Message);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            
            throw;
        }
    }
}
