using Slck.Envelope.Contracts;

namespace Slck.Envelope.Factory
{
    public static class EnvelopeFactory
    {
        public static ApiResponse<T> Ok<T>(T data, PaginationMeta? meta = null) => ApiResponse<T>.Ok(data, meta);
        public static ApiResponse<T> NoContent<T>() => ApiResponse<T>.NoContent();
        public static ApiResponse<T> Fail<T>(string code, string message, IDictionary<string, string[]?>? details = null) =>
            ApiResponse<T>.Fail(code, message, details);
    }
}
