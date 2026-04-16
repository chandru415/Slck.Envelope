using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Slck.Envelope.Options;

namespace Slck.Envelope.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers envelope options so middleware and results can be configured centrally.
        /// Calling this is optional — all options have safe defaults.
        /// </summary>
        public static IServiceCollection AddApiEnvelope(
            this IServiceCollection services,
            Action<EnvelopeOptions>? configure = null)
        {
            if (configure is not null)
                services.Configure<EnvelopeOptions>(configure);

            return services;
        }

        /// <summary>
        /// Adds <see cref="EnvelopeActionFilter"/> as a global MVC filter so that all
        /// controller actions automatically return envelope-shaped responses.
        /// Pair this with <c>app.UseApiEnvelope()</c> for complete coverage.
        /// </summary>
        public static IMvcBuilder AddEnvelopeFilter(this IMvcBuilder builder)
        {
            builder.Services.Configure<MvcOptions>(o => o.Filters.Add<EnvelopeActionFilter>());
            return builder;
        }
    }
}
