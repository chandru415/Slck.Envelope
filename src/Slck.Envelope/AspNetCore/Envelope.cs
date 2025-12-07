

using Microsoft.AspNetCore.Http;
using Slck.Envelope.Contracts;

namespace Slck.Envelope.AspNetCore
{
    public static class Envelope
    {
        // ✅ Success
        public static IResult Ok<T>(T data, PaginationMeta? meta = null) =>
            new EnvelopeResult<T>(ApiResponse<T>.Ok(data, meta), StatusCodes.Status200OK);

        public static IResult Created<T>(string location, T data) =>
            new EnvelopeResult<T>(ApiResponse<T>.Ok(data), StatusCodes.Status201Created, location);

        public static IResult NoContent() =>
            new EnvelopeResult<object>(ApiResponse<object>.NoContent(), StatusCodes.Status204NoContent);

        // ❌ Client errors
        public static IResult BadRequest(string message = "Bad request", IDictionary<string, string[]?>? details = null) =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("bad_request", message, details), StatusCodes.Status400BadRequest);

        public static IResult Unauthorized(string message = "Unauthorized") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("unauthorized", message), StatusCodes.Status401Unauthorized);

        public static IResult Forbidden(string message = "Forbidden") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("forbidden", message), StatusCodes.Status403Forbidden);

        public static IResult NotFound(string message = "Resource not found") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("not_found", message), StatusCodes.Status404NotFound);

        public static IResult Conflict(string message = "Conflict") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("conflict", message), StatusCodes.Status409Conflict);

        public static IResult UnprocessableEntity(IDictionary<string, string[]?> errors, string message = "Validation failed") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("validation_error", message, errors), StatusCodes.Status422UnprocessableEntity);

        public static IResult TooManyRequests(string message = "Rate limit exceeded") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("too_many_requests", message), StatusCodes.Status429TooManyRequests);

        // ⚠️ Server errors
        public static IResult Error(string message = "An error occurred", string code = "server_error") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail(code, message), StatusCodes.Status500InternalServerError);

        public static IResult ServiceUnavailable(string message = "Service unavailable") =>
            new EnvelopeResult<object>(ApiResponse<object>.Fail("service_unavailable", message), StatusCodes.Status503ServiceUnavailable);
    }
}
