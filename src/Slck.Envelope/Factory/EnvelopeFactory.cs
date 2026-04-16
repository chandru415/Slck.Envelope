using Slck.Envelope.Contracts;

namespace Slck.Envelope.Factory
{
    public static class EnvelopeFactory
    {
        public static ApiResponse<T> Ok<T>(T data, PaginationMeta? meta = null, string? requestId = null) =>
            ApiResponse<T>.Ok(data, meta, requestId);

        public static ApiResponse<T> NoContent<T>(string? requestId = null) =>
            ApiResponse<T>.NoContent(requestId);

        public static ApiResponse<T> Fail<T>(string code, string message, IDictionary<string, string[]?>? details = null, string? requestId = null) =>
            ApiResponse<T>.Fail(code, message, details, requestId);
    }
}
