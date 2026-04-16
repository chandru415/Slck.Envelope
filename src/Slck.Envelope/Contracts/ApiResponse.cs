
using System.Text.Json.Serialization;

namespace Slck.Envelope.Contracts
{
    public record PaginationMeta
    {
        /// <summary>Current 1-based page number.</summary>
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long Total { get; init; }
        /// <summary>
        /// True when more pages exist. Uses 1-based page index:
        /// items delivered so far = Page * PageSize.
        /// </summary>
        public bool HasMore => Page * (long)PageSize < Total;
    }

    public record ErrorInfo
    {
        public string Code { get; init; } = "error";
        public string Message { get; init; } = string.Empty;
        public IDictionary<string, string[]?>? Details { get; init; }
    }

    public record ApiResponse<T>
    {
        /// <summary>Indicates whether the request succeeded.</summary>
        [JsonPropertyOrder(0)]
        public bool Success { get; init; }

        /// <summary>Response payload. Present on success responses.</summary>
        [JsonPropertyOrder(1)]
        public T? Data { get; init; }

        /// <summary>Structured error info. Present on failure responses.</summary>
        [JsonPropertyOrder(2)]
        public ErrorInfo? Error { get; init; }

        /// <summary>Pagination metadata for collection responses.</summary>
        [JsonPropertyOrder(3)]
        public PaginationMeta? Meta { get; init; }

        /// <summary>Correlation / request ID echoed from the incoming header.</summary>
        [JsonPropertyOrder(4)]
        public string? RequestId { get; init; }

        /// <summary>UTC timestamp when the response was produced.</summary>
        [JsonPropertyOrder(5)]
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        public static ApiResponse<T> Ok(T data, PaginationMeta? meta = null, string? requestId = null) =>
            new() { Success = true, Data = data, Meta = meta, RequestId = requestId };

        public static ApiResponse<T> NoContent(string? requestId = null) =>
            new() { Success = true, Data = default, Meta = null, RequestId = requestId };

        public static ApiResponse<T> Fail(string code, string message, IDictionary<string, string[]?>? details = null, string? requestId = null) =>
            new() { Success = false, Error = new ErrorInfo { Code = code, Message = message, Details = details }, RequestId = requestId };
    }
}
