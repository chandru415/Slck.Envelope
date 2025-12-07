using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Slck.Envelope.Contracts;
using Slck.Envelope.Utils;
using System.Text.Json;

public class ApiEnvelopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiEnvelopeMiddleware> _logger;

    public ApiEnvelopeMiddleware(RequestDelegate next, ILogger<ApiEnvelopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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

public static class ApiEnvelopeMiddlewareExtensions
{
    public static IApplicationBuilder UseApiEnvelope(this IApplicationBuilder app) =>
        app.UseMiddleware<ApiEnvelopeMiddleware>();
}
