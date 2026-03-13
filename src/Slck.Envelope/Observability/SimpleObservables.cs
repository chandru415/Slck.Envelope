using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Slck.Envelope.Observability;

/// <summary>
/// Ultra-simplified base class for commands - ZERO constructor parameters needed!
/// Dependencies are resolved automatically from HttpContext.
/// </summary>
public abstract class AutoObservableCommand<TResult> : IObservableCommand<TResult>, IObservableHandler<IResult>
{
    private IHttpContextAccessor? _httpContextAccessor;
    private ILogger? _logger;
    private ActivitySource? _activitySource;
    private SlckEnvelopeObservabilityOptions? _options;

    protected AutoObservableCommand(IHttpContextAccessor httpContextAccessor)
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

    public async Task<IResult> ExecuteAsync()
    {
        return await ObservableHandlerExecutor.ExecuteCommandAsync(this, Options);
    }

    public abstract Task<IResult> HandleAsync();
}

/// <summary>
/// Ultra-simplified base class for queries - ZERO constructor parameters needed!
/// Dependencies are resolved automatically from HttpContext.
/// </summary>
public abstract class AutoObservableQuery<TResult> : IObservableQuery<TResult>, IObservableHandler<IResult>
{
    private IHttpContextAccessor? _httpContextAccessor;
    private ILogger? _logger;
    private ActivitySource? _activitySource;
    private SlckEnvelopeObservabilityOptions? _options;

    protected AutoObservableQuery(IHttpContextAccessor httpContextAccessor)
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

    public async Task<IResult> ExecuteAsync()
    {
        return await ObservableHandlerExecutor.ExecuteQueryAsync(this, Options);
    }

    public abstract Task<IResult> HandleAsync();
}
