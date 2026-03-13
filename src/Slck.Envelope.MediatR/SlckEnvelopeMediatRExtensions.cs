using System.Diagnostics;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Slck.Envelope.Observability;

namespace Slck.Envelope.MediatR;

/// <summary>
/// Extension methods for registering Slck.Envelope observability with MediatR.
/// </summary>
public static class SlckEnvelopeMediatRExtensions
{
    /// <summary>
    /// Adds MediatR with automatic Slck.Envelope observability (OTEL + Serilog).
    /// Automatically reads configuration from appsettings.json "SlckEnvelope:Observability" section.
    /// This registers MediatR and adds the observability pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration to read observability settings from</param>
    /// <param name="configure">Optional MediatR configuration action</param>
    public static IServiceCollection AddSlckEnvelopeMediatR(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<MediatRServiceConfiguration>? configure = null)
    {
        // Register HttpContextAccessor for Auto* handlers
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Register observability infrastructure first
        services.AddSlckEnvelopeObservability(configuration);

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            // Apply user configuration first
            configure?.Invoke(cfg);
            
            // Add observability behavior (runs for all requests)
            cfg.AddOpenBehavior(typeof(Behaviors.ObservabilityPipelineBehavior<,>));
        });

        return services;
    }

    /// <summary>
    /// Adds MediatR with automatic Slck.Envelope observability (OTEL + Serilog).
    /// Scans specified assemblies for handlers.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration to read observability settings from</param>
    /// <param name="assemblies">Assemblies to scan for MediatR handlers</param>
    public static IServiceCollection AddSlckEnvelopeMediatR(
        this IServiceCollection services,
        IConfiguration? configuration,
        params Assembly[] assemblies)
    {
        return services.AddSlckEnvelopeMediatR(configuration, cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });
    }

    /// <summary>
    /// Adds MediatR with automatic Slck.Envelope observability (OTEL + Serilog).
    /// Scans specified assemblies for handlers (without configuration).
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for MediatR handlers</param>
    public static IServiceCollection AddSlckEnvelopeMediatR(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddSlckEnvelopeMediatR(null, cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });
    }

    /// <summary>
    /// Adds observability pipeline behavior to existing MediatR configuration.
    /// Use this if you already called AddMediatR() and want to add observability.
    /// Automatically reads configuration from appsettings.json.
    /// </summary>
    public static IServiceCollection AddMediatRObservability(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Register HttpContextAccessor for Auto* handlers
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Register observability infrastructure first
        services.AddSlckEnvelopeObservability(configuration);

        // Add the pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.ObservabilityPipelineBehavior<,>));
        
        return services;
    }
}
