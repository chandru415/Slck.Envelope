using Microsoft.AspNetCore.Http;
using Slck.Envelope.Contracts;
using Slck.Envelope.Utils;
using System.Text.Json;

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
        httpContext.Response.StatusCode = _statusCode;

        if (!string.IsNullOrEmpty(_location))
        {
            httpContext.Response.Headers.Location = _location;
        }

        if (_statusCode == StatusCodes.Status204NoContent)
        {
            httpContext.Response.ContentLength = 0;
            return;
        }

        // 🔑 Stamp request ID if missing
        var enriched = _payload with
        {
            RequestId = _payload.RequestId ?? httpContext.TraceIdentifier,
            Timestamp = DateTimeOffset.UtcNow
        };

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, enriched, JsonSerializerOptionsProvider.Default, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
