using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Slck.Envelope.Contracts;
using Slck.Envelope.Options;
using Slck.Envelope.Utils;
using System.Text.Json;

namespace Slck.Envelope.AspNetCore
{
    public sealed class EnvelopeResult<T> : IResult
    {
        private readonly ApiResponse<T> _payload;
        private readonly int _statusCode;
        private readonly string? _location;

        public EnvelopeResult(ApiResponse<T> payload, int statusCode = StatusCodes.Status200OK, string? location = null)
        {
            _payload = payload;
            _statusCode = statusCode;
            _location = location;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var options = httpContext.RequestServices
                .GetService<IOptions<EnvelopeOptions>>()?.Value ?? new EnvelopeOptions();

            // Prefer the incoming correlation header; fall back to TraceIdentifier
            var correlationId = httpContext.Request.Headers.TryGetValue(options.CorrelationIdHeader, out var header)
                ? header.ToString()
                : httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = _statusCode;
            httpContext.Response.Headers[options.CorrelationIdHeader] = correlationId;

            if (!string.IsNullOrEmpty(_location))
                httpContext.Response.Headers.Location = _location;

            if (_statusCode == StatusCodes.Status204NoContent)
            {
                httpContext.Response.ContentLength = 0;
                return;
            }

            var enriched = _payload with
            {
                RequestId = correlationId,
                Timestamp = DateTimeOffset.UtcNow
            };

            var jsonOptions = options.JsonSerializerOptions ?? JsonSerializerOptionsProvider.Default;
            httpContext.Response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, enriched, jsonOptions, httpContext.RequestAborted)
                .ConfigureAwait(false);
        }
    }
}
