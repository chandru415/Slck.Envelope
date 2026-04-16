using Microsoft.AspNetCore.Http;
using Slck.Envelope.Contracts;
using System.Text.Json;

namespace Slck.Envelope.Options
{
    /// <summary>
    /// Describes how a specific exception type should be translated into an HTTP response.
    /// </summary>
    public record ExceptionMapping
    {
        /// <summary>HTTP status code to return.</summary>
        public int StatusCode { get; init; }

        /// <summary>Machine-readable error code written to the envelope <c>error.code</c> field.</summary>
        public string ErrorCode { get; init; } = "error";

        /// <summary>
        /// Optional override message. When null the middleware uses the exception message
        /// (or a generic message if <see cref="EnvelopeOptions.IncludeExceptionDetails"/> is false).
        /// </summary>
        public string? Message { get; init; }

        public ExceptionMapping(int statusCode, string errorCode, string? message = null)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Message = message;
        }
    }

    public class EnvelopeOptions
    {
        /// <summary>
        /// When true, the exception type and message are included in 500 responses.
        /// Should only be enabled in Development environments.
        /// </summary>
        public bool IncludeExceptionDetails { get; set; } = false;

        /// <summary>
        /// HTTP header name used to propagate a correlation / request ID across services.
        /// The middleware reads this header from the incoming request and echoes it in the
        /// response. Falls back to HttpContext.TraceIdentifier when absent.
        /// Defaults to "X-Correlation-ID".
        /// </summary>
        public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

        /// <summary>
        /// Custom <see cref="JsonSerializerOptions"/> applied to all envelope responses.
        /// When null, the built-in defaults are used: camelCase, compact, nulls omitted.
        /// </summary>
        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        /// <summary>
        /// Maps exception types to specific HTTP status codes and error codes.
        /// The middleware walks the runtime type hierarchy, so registering a base exception
        /// type also covers derived types. Exact type match takes priority.
        /// </summary>
        public Dictionary<Type, ExceptionMapping> ExceptionMap { get; } = new();

        /// <summary>
        /// Fluent helper to register an exception mapping.
        /// </summary>
        public EnvelopeOptions MapException<TException>(int statusCode, string errorCode, string? message = null)
            where TException : Exception
        {
            ExceptionMap[typeof(TException)] = new ExceptionMapping(statusCode, errorCode, message);
            return this;
        }

        /// <summary>
        /// Optional hook called just before the error envelope is serialized to the response.
        /// Use this to mutate or enrich the <see cref="ApiResponse{T}"/> —
        /// for example to attach an OpenTelemetry <c>traceId</c> or tenant context.
        /// The callback receives the original exception and may return a replacement response;
        /// return null to keep the default response unchanged.
        /// </summary>
        public Func<Exception, ApiResponse<object>, ApiResponse<object>>? OnBeforeWriteError { get; set; }

        /// <summary>
        /// Resolves the best <see cref="ExceptionMapping"/> for the given exception, walking
        /// the type hierarchy from most-derived to least-derived.
        /// Returns null when no mapping is found.
        /// </summary>
        internal ExceptionMapping? Resolve(Exception ex)
        {
            var type = ex.GetType();
            while (type is not null && type != typeof(object))
            {
                if (ExceptionMap.TryGetValue(type, out var mapping))
                    return mapping;
                type = type.BaseType;
            }
            return null;
        }
    }
}
