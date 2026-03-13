using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.Contracts;
using Slck.Envelope.Utils;
using System.Text.Json;

namespace Slck.Envelope.AspNetCore
{
    public class ApiEnvelopeMiddleware(RequestDelegate next, ILogger<ApiEnvelopeMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ApiEnvelopeMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception {RequestId} {Method} {Path}",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started; cannot write envelope.");
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";

                var payload = ApiResponse<object>.Fail(
                    "server_error",
                    "An unexpected error occurred",
                    requestId: context.TraceIdentifier
                );

                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    payload,
                    JsonSerializerOptionsProvider.Default,
                    context.RequestAborted
                ).ConfigureAwait(false);
            }
        }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    using Slck.Envelope.AspNetCore;

    public static class SlckEnvelopeApplicationBuilderExtensions
    {
        // Preferred names
        public static IApplicationBuilder UseSlckEnvelope(this IApplicationBuilder app)
        {
            // Ensure request id middleware is registered first
            app.UseRequestId();
            return app.UseMiddleware<ApiEnvelopeMiddleware>();
        }

        public static IApplicationBuilder UseEnvelope(this IApplicationBuilder app) =>
            UseSlckEnvelope(app);

        // Back-compat shim
        [Obsolete("Use UseSlckEnvelope or UseEnvelope instead.")]
        public static IApplicationBuilder UseApiEnvelope(this IApplicationBuilder app) =>
            UseSlckEnvelope(app);

        // WebApplication-friendly overloads for minimal APIs
        public static WebApplication UseSlckEnvelope(this WebApplication app)
        {
            // Ensure request id middleware is registered first
            app.UseRequestId();
            app.UseMiddleware<ApiEnvelopeMiddleware>();
            return app;
        }

        public static WebApplication UseEnvelope(this WebApplication app) =>
            UseSlckEnvelope(app);

        [Obsolete("Use UseSlckEnvelope or UseEnvelope instead.")]
        public static WebApplication UseApiEnvelope(this WebApplication app) =>
            UseSlckEnvelope(app);
    }
}
