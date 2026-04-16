using Microsoft.AspNetCore.Http;
using Slck.Envelope.AspNetCore;

namespace Slck.Envelope.Contracts
{
    /// <summary>
    /// Extension methods that bridge <see cref="ApiResponse{T}"/> (usable anywhere, including
    /// service/domain layers) with ASP.NET Core's <see cref="IResult"/> for Minimal APIs.
    /// </summary>
    public static class ApiResponseExtensions
    {
        /// <summary>
        /// Converts any <see cref="ApiResponse{T}"/> to an <see cref="IResult"/> with an
        /// explicit HTTP status code.
        /// </summary>
        public static IResult ToResult<T>(this ApiResponse<T> response, int statusCode) =>
            new EnvelopeResult<T>(response, statusCode);

        /// <summary>Returns a 200 OK result.</summary>
        public static IResult ToOkResult<T>(this ApiResponse<T> response) =>
            new EnvelopeResult<T>(response, StatusCodes.Status200OK);

        /// <summary>Returns a 201 Created result with an optional Location header.</summary>
        public static IResult ToCreatedResult<T>(this ApiResponse<T> response, string? location = null) =>
            new EnvelopeResult<T>(response, StatusCodes.Status201Created, location);

        /// <summary>Returns a 202 Accepted result with an optional status-poll URL as the Location header.</summary>
        public static IResult ToAcceptedResult<T>(this ApiResponse<T> response, string? statusUrl = null) =>
            new EnvelopeResult<T>(response, StatusCodes.Status202Accepted, statusUrl);

        /// <summary>Returns a 400 Bad Request result.</summary>
        public static IResult ToBadRequestResult<T>(this ApiResponse<T> response) =>
            new EnvelopeResult<T>(response, StatusCodes.Status400BadRequest);

        /// <summary>Returns a 404 Not Found result.</summary>
        public static IResult ToNotFoundResult<T>(this ApiResponse<T> response) =>
            new EnvelopeResult<T>(response, StatusCodes.Status404NotFound);

        /// <summary>Returns a 422 Unprocessable Entity result.</summary>
        public static IResult ToUnprocessableResult<T>(this ApiResponse<T> response) =>
            new EnvelopeResult<T>(response, StatusCodes.Status422UnprocessableEntity);
    }
}
