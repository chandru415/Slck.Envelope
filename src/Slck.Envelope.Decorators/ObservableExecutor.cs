using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Slck.Envelope.Observability;

namespace Slck.Envelope.Decorators;

/// <summary>
/// Marker interface for classes that should have automatic OTEL and Serilog instrumentation.
/// Any class implementing this interface will get automatic observability when using ObservableExecutor.
/// </summary>
public interface IObservable
{
    // Marker interface - no members required
}

/// <summary>
/// Static executor that wraps ANY method execution with automatic OTEL tracing and Serilog logging.
/// Use this to add observability to any class without modifying its code.
/// Respects configuration from appsettings.json.
/// </summary>
public static class ObservableExecutor
{
    /// <summary>
    /// Executes an action with automatic OTEL tracing and Serilog logging.
    /// </summary>
    public static void Execute(
        ILogger logger,
        ActivitySource activitySource,
        string operationName,
        Action action,
        SlckEnvelopeObservabilityOptions? options = null,
        Dictionary<string, object>? tags = null)
    {
        var enabled = options?.Enabled ?? true;
        if (!enabled)
        {
            action();
            return;
        }

        ExecuteInternal(
            () => { action(); return Task.CompletedTask; },
            logger,
            activitySource,
            operationName,
            options,
            tags).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an async action with automatic OTEL tracing and Serilog logging.
    /// </summary>
    public static async Task ExecuteAsync(
        ILogger logger,
        ActivitySource activitySource,
        string operationName,
        Func<Task> action,
        SlckEnvelopeObservabilityOptions? options = null,
        Dictionary<string, object>? tags = null)
    {
        var enabled = options?.Enabled ?? true;
        if (!enabled)
        {
            await action();
            return;
        }

        await ExecuteInternal(action, logger, activitySource, operationName, options, tags);
    }

    /// <summary>
    /// Executes a function with automatic OTEL tracing and Serilog logging, returning a result.
    /// </summary>
    public static TResult Execute<TResult>(
        ILogger logger,
        ActivitySource activitySource,
        string operationName,
        Func<TResult> func,
        SlckEnvelopeObservabilityOptions? options = null,
        Dictionary<string, object>? tags = null)
    {
        var enabled = options?.Enabled ?? true;
        if (!enabled)
        {
            return func();
        }

        return ExecuteInternalWithResult(
            () => Task.FromResult(func()),
            logger,
            activitySource,
            operationName,
            options,
            tags).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes an async function with automatic OTEL tracing and Serilog logging, returning a result.
    /// </summary>
    public static async Task<TResult> ExecuteAsync<TResult>(
        ILogger logger,
        ActivitySource activitySource,
        string operationName,
        Func<Task<TResult>> func,
        SlckEnvelopeObservabilityOptions? options = null,
        Dictionary<string, object>? tags = null)
    {
        var enabled = options?.Enabled ?? true;
        if (!enabled)
        {
            return await func();
        }

        return await ExecuteInternalWithResult(func, logger, activitySource, operationName, options, tags);
    }

    private static async Task ExecuteInternal(
        Func<Task> action,
        ILogger logger,
        ActivitySource activitySource,
        string operationName,
        SlckEnvelopeObservabilityOptions? options,
        Dictionary<string, object>? tags)
    {
        var enableTracing = options?.EnableAutoTracing ?? true;
        var enableLogging = options?.EnableSerilogEnrichment ?? true;

        Activity? activity = null;
        if (enableTracing)
        {
            activity = activitySource.StartActivity($"Operation.{operationName}", ActivityKind.Internal);
            activity?.SetTag("operation.name", operationName);

            // Add default tags from configuration
            if (options?.DefaultTags != null)
            {
                foreach (var tag in options.DefaultTags)
                {
                    activity?.SetTag(tag.Key, tag.Value);
                }
            }

            // Add custom tags
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    activity?.SetTag(tag.Key, tag.Value);
                }
            }
        }

        try
        {
            if (enableLogging)
            {
                using (logger.BeginScope(new Dictionary<string, object>
                {
                    ["OperationName"] = operationName,
                    ["TraceId"] = activity?.TraceId.ToString() ?? "none",
                    ["SpanId"] = activity?.SpanId.ToString() ?? "none"
                }))
                {
                    logger.LogInformation("Executing operation: {OperationName}", operationName);
                    await action();
                    logger.LogInformation("Operation {OperationName} completed successfully", operationName);
                }
            }
            else
            {
                await action();
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation {OperationName} failed: {ErrorMessage}", operationName, ex.Message);
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

    private static async Task<TResult> ExecuteInternalWithResult<TResult>(
        Func<Task<TResult>> func,
        ILogger logger,
        ActivitySource activitySource,
        string operationName,
        SlckEnvelopeObservabilityOptions? options,
        Dictionary<string, object>? tags)
    {
        var enableTracing = options?.EnableAutoTracing ?? true;
        var enableLogging = options?.EnableSerilogEnrichment ?? true;

        Activity? activity = null;
        if (enableTracing)
        {
            activity = activitySource.StartActivity($"Operation.{operationName}", ActivityKind.Internal);
            activity?.SetTag("operation.name", operationName);

            // Add default tags from configuration
            if (options?.DefaultTags != null)
            {
                foreach (var tag in options.DefaultTags)
                {
                    activity?.SetTag(tag.Key, tag.Value);
                }
            }

            // Add custom tags
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    activity?.SetTag(tag.Key, tag.Value);
                }
            }
        }

        try
        {
            TResult result;
            if (enableLogging)
            {
                using (logger.BeginScope(new Dictionary<string, object>
                {
                    ["OperationName"] = operationName,
                    ["TraceId"] = activity?.TraceId.ToString() ?? "none",
                    ["SpanId"] = activity?.SpanId.ToString() ?? "none"
                }))
                {
                    logger.LogInformation("Executing operation: {OperationName}", operationName);
                    result = await func();
                    logger.LogInformation("Operation {OperationName} completed successfully", operationName);
                }
            }
            else
            {
                result = await func();
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation {OperationName} failed: {ErrorMessage}", operationName, ex.Message);
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
