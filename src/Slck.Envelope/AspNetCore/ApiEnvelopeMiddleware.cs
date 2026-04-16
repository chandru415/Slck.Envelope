using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slck.Envelope.Contracts;
using Slck.Envelope.Options;
using Slck.Envelope.Utils;
using System.Text.Json;

namespace Slck.Envelope.AspNetCore
{
    public class ApiEnvelopeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiEnvelopeMiddleware> _logger;
        private readonly EnvelopeOptions _options;

        public ApiEnvelopeMiddleware(
            RequestDelegate next,
            ILogger<ApiEnvelopeMiddleware> logger,
            IOptions<EnvelopeOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started; cannot write envelope.");
                    throw;
                }

                // Check if the endpoint opted out of envelope exception handling
                var skipMeta = context.GetEndpoint()?.Metadata.GetMetadata<ISkipEnvelopeMetadata>();
                if (skipMeta is not null)
                    throw;

                // Resolve mapping first so we can choose the right log level in one pass
                var mapped = _options.Resolve(ex);

                if (mapped is not null)
                    _logger.LogWarning(ex,
                        "Mapped exception {ExceptionType} → {StatusCode} {ErrorCode} | {RequestId} {Method} {Path}",
                        ex.GetType().Name, mapped.StatusCode, mapped.ErrorCode,
                        context.TraceIdentifier, context.Request.Method, context.Request.Path);
                else
                    _logger.LogError(ex,
                        "Unhandled exception {RequestId} {Method} {Path}",
                        context.TraceIdentifier, context.Request.Method, context.Request.Path);

                var correlationId = context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var header)
                    ? header.ToString()
                    : context.TraceIdentifier;

                var statusCode = mapped?.StatusCode ?? StatusCodes.Status500InternalServerError;
                var errorCode = mapped?.ErrorCode ?? "server_error";
                var message = mapped?.Message
                    ?? (_options.IncludeExceptionDetails ? $"{ex.GetType().Name}: {ex.Message}" : "An unexpected error occurred");

                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.Headers[_options.CorrelationIdHeader] = correlationId;

                var payload = ApiResponse<object>.Fail(errorCode, message, requestId: correlationId);

                // Allow consumers to enrich or replace the error envelope via a hook
                var finalPayload = _options.OnBeforeWriteError?.Invoke(ex, payload) ?? payload;

                var jsonOptions = _options.JsonSerializerOptions ?? JsonSerializerOptionsProvider.Default;
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    finalPayload,
                    jsonOptions,
                    context.RequestAborted
                ).ConfigureAwait(false);
            }
        }
    }

    public static class ApiEnvelopeMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiEnvelope(this IApplicationBuilder app) =>
            app.UseMiddleware<ApiEnvelopeMiddleware>();
    }
}
