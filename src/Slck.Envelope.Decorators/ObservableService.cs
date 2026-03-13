using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slck.Envelope.Observability;

namespace Slck.Envelope.Decorators;

/// <summary>
/// Base class for services that need automatic OTEL and Serilog instrumentation.
/// Inherit from this class to get Logger and ActivitySource injected automatically.
/// Respects configuration from appsettings.json.
/// </summary>
public abstract class ObservableService : IObservable
{
    protected ILogger Logger { get; }
    protected ActivitySource ActivitySource { get; }
    protected SlckEnvelopeObservabilityOptions? Options { get; }

    /// <summary>
    /// Initializes a new instance of ObservableService.
    /// </summary>
    /// <param name="logger">Logger instance for this service</param>
    /// <param name="activitySource">ActivitySource for OTEL tracing</param>
    /// <param name="options">OPTIONAL: Configuration options. If null, observability is enabled by default.
    /// Inject this parameter ONLY if you need configuration control from appsettings.json.
    /// For most cases, you can omit this parameter.</param>
    protected ObservableService(
        ILogger logger, 
        ActivitySource activitySource,
        SlckEnvelopeObservabilityOptions? options = null)
    {
        Logger = logger;
        ActivitySource = activitySource;
        Options = options;
    }

    /// <summary>
    /// Execute a method with automatic OTEL tracing and Serilog logging.
    /// </summary>
    protected void ExecuteObservable(
        string operationName, 
        Action action, 
        Dictionary<string, object>? tags = null)
    {
        ObservableExecutor.Execute(Logger, ActivitySource, operationName, action, Options, tags);
    }

    /// <summary>
    /// Execute an async method with automatic OTEL tracing and Serilog logging.
    /// </summary>
    protected async Task ExecuteObservableAsync(
        string operationName, 
        Func<Task> action, 
        Dictionary<string, object>? tags = null)
    {
        await ObservableExecutor.ExecuteAsync(Logger, ActivitySource, operationName, action, Options, tags);
    }

    /// <summary>
    /// Execute a method with automatic OTEL tracing and Serilog logging, returning a result.
    /// </summary>
    protected TResult ExecuteObservable<TResult>(
        string operationName, 
        Func<TResult> func, 
        Dictionary<string, object>? tags = null)
    {
        return ObservableExecutor.Execute(Logger, ActivitySource, operationName, func, Options, tags);
    }

    /// <summary>
    /// Execute an async method with automatic OTEL tracing and Serilog logging, returning a result.
    /// </summary>
    protected async Task<TResult> ExecuteObservableAsync<TResult>(
        string operationName, 
        Func<Task<TResult>> func, 
        Dictionary<string, object>? tags = null)
    {
        return await ObservableExecutor.ExecuteAsync(Logger, ActivitySource, operationName, func, Options, tags);
    }
}

/// <summary>
/// Ultra-simplified base class for services - ONLY inject IHttpContextAccessor!
/// Logger, ActivitySource, and Options are resolved automatically.
/// ?? WARNING: Only works in HTTP request context (not for background jobs)!
/// </summary>
public abstract class AutoObservableService : IObservable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ILogger? _logger;
    private ActivitySource? _activitySource;
    private SlckEnvelopeObservabilityOptions? _options;

    protected AutoObservableService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected ILogger Logger => _logger ??= GetService<ILoggerFactory>().CreateLogger(GetType());
    protected ActivitySource ActivitySource => _activitySource ??= GetService<ActivitySource>();
    protected SlckEnvelopeObservabilityOptions? Options => _options ??= GetService<SlckEnvelopeObservabilityOptions>();

    private T GetService<T>() where T : notnull
    {
        var services = _httpContextAccessor?.HttpContext?.RequestServices;
        if (services == null)
            throw new InvalidOperationException("HttpContext not available. Use ObservableService for background jobs/console apps.");
        
        return services.GetRequiredService<T>();
    }

    /// <summary>
    /// Execute a method with automatic OTEL tracing and Serilog logging.
    /// </summary>
    protected void ExecuteObservable(
        string operationName, 
        Action action, 
        Dictionary<string, object>? tags = null)
    {
        ObservableExecutor.Execute(Logger, ActivitySource, operationName, action, Options, tags);
    }

    /// <summary>
    /// Execute an async method with automatic OTEL tracing and Serilog logging.
    /// </summary>
    protected async Task ExecuteObservableAsync(
        string operationName, 
        Func<Task> action, 
        Dictionary<string, object>? tags = null)
    {
        await ObservableExecutor.ExecuteAsync(Logger, ActivitySource, operationName, action, Options, tags);
    }

    /// <summary>
    /// Execute a method with automatic OTEL tracing and Serilog logging, returning a result.
    /// </summary>
    protected TResult ExecuteObservable<TResult>(
        string operationName, 
        Func<TResult> func, 
        Dictionary<string, object>? tags = null)
    {
        return ObservableExecutor.Execute(Logger, ActivitySource, operationName, func, Options, tags);
    }

    /// <summary>
    /// Execute an async method with automatic OTEL tracing and Serilog logging, returning a result.
    /// </summary>
    protected async Task<TResult> ExecuteObservableAsync<TResult>(
        string operationName, 
        Func<Task<TResult>> func, 
        Dictionary<string, object>? tags = null)
    {
        return await ObservableExecutor.ExecuteAsync(Logger, ActivitySource, operationName, func, Options, tags);
    }
}
