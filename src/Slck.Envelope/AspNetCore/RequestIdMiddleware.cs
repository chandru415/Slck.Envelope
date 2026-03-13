using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Slck.Envelope.AspNetCore
{
    /// <summary>
    /// Middleware that ensures a consistent request ID throughout the request pipeline.
    /// Reads X-Request-Id header if present, otherwise generates one from Activity or TraceIdentifier.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public class RequestIdMiddleware(RequestDelegate next)
    {
        private const string RequestIdHeader = "X-Request-Id";

        /// <summary>
        /// Processes the HTTP request to ensure request ID propagation.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            // Prefer incoming header, fallback to activity trace or built-in TraceIdentifier
            var requestId = context.Request.Headers.TryGetValue(RequestIdHeader, out var values) 
                            && !string.IsNullOrWhiteSpace(values)
                ? values.ToString()
                : Activity.Current?.Id ?? context.TraceIdentifier;

            // Set TraceIdentifier so downstream middleware and EnvelopeResult can use it
            context.TraceIdentifier = requestId;

            // Ensure response header contains request id
            context.Response.OnStarting(state =>
            {
                var (ctx, id) = ((HttpContext, string))state;
                if (!ctx.Response.Headers.ContainsKey(RequestIdHeader))
                {
                    ctx.Response.Headers[RequestIdHeader] = id;
                }
                return Task.CompletedTask;
            }, (context, requestId));

            await next(context).ConfigureAwait(false);
        }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    using Slck.Envelope.AspNetCore;

    /// <summary>
    /// Extension methods for adding request ID middleware to the application pipeline.
    /// </summary>
    public static class RequestIdMiddlewareExtensions
    {
        /// <summary>
        /// Adds the request ID middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseRequestId(this IApplicationBuilder app) => app.UseMiddleware<RequestIdMiddleware>();

        /// <summary>
        /// Adds the request ID middleware to the web application pipeline.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <returns>The web application for chaining.</returns>
        public static WebApplication UseRequestId(this WebApplication app)
        {
            app.UseMiddleware<RequestIdMiddleware>();
            return app;
        }
    }
}
