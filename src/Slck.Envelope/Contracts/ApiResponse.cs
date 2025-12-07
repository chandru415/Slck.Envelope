
namespace Slck.Envelope.Contracts
{
    public record PaginationMeta
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long Total { get; init; }
        public bool HasMore => (Page + 1) * (long)PageSize < Total;
    }

    public record ErrorInfo
    {
        public string Code { get; init; } = "error";
        public string Message { get; init; } = string.Empty;
        public IDictionary<string, string[]?>? Details { get; init; }
    }

    public record ApiResponse<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public ErrorInfo? Error { get; init; }
        public PaginationMeta? Meta { get; init; }

        // 🔑 New fields for observability
        public string? RequestId { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        public static ApiResponse<T> Ok(T data, PaginationMeta? meta = null, string? requestId = null) =>
            new() { Success = true, Data = data, Meta = meta, RequestId = requestId };

        public static ApiResponse<T> NoContent(string? requestId = null) =>
            new() { Success = true, Data = default, Meta = null, RequestId = requestId };

        public static ApiResponse<T> Fail(string code, string message, IDictionary<string, string[]?>? details = null, string? requestId = null) =>
            new() { Success = false, Error = new ErrorInfo { Code = code, Message = message, Details = details }, RequestId = requestId };
    }
}
