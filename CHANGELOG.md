# Changelog

All notable changes to **Slck.Envelope** are documented here.
This project follows [Semantic Versioning](https://semver.org/).

---

## [1.2.0] — 2026-04-16

### Added
- **Exception → status code mapping** (`EnvelopeOptions.MapException<T>`) — translate domain exceptions into specific HTTP status codes and error codes without try/catch in handlers. Type-hierarchy aware (base-type registration covers derived types).
- **`[SkipEnvelope]` opt-out attribute** — decorate a Minimal API endpoint (`.WithMetadata(new SkipEnvelopeAttribute())`) or an MVC controller/action to bypass envelope exception handling entirely.
- **`EnvelopeActionFilter`** — MVC global action filter that automatically wraps `ObjectResult` responses from controllers into `ApiResponse<T>`. Register with `AddControllers().AddEnvelopeFilter()`.
- **`ServiceCollectionExtensions.AddEnvelopeFilter()`** — fluent registration of the MVC action filter.
- **`Envelope.Accepted<T>(data, statusUrl?)`** — 202 Accepted with optional `Location` header for async / job-queue patterns.
- **`ApiResponseExtensions`** — `ToResult()`, `ToOkResult()`, `ToCreatedResult()`, `ToAcceptedResult()`, `ToBadRequestResult()`, `ToNotFoundResult()`, `ToUnprocessableResult()` — bridge `ApiResponse<T>` from domain/service layers to `IResult` without an ASP.NET Core dependency.
- **`EnvelopeOptions.OnBeforeWriteError`** — hook invoked just before the error envelope is serialized; allows consumers to attach OpenTelemetry `traceId`, tenant ID, or any custom field.
- **`[JsonPropertyOrder]` on `ApiResponse<T>`** — guarantees stable JSON field order: `success → data → error → meta → requestId → timestamp`.

### Fixed
- **`PaginationMeta.HasMore` logic bug** — was using 0-based page formula `(Page + 1) * PageSize`; corrected to 1-based `Page * PageSize < Total`.
- **Duplicate exception log entries** — middleware was logging at `Error` unconditionally before resolving the exception map, producing two log entries per mapped exception. Now resolves the map first: mapped exceptions log once at `Warning`; unmapped exceptions log once at `Error`.
- **`EnvelopeResult<T>` global namespace** — moved into `Slck.Envelope.AspNetCore` namespace for consistency.
- **`EnvelopeFactory` missing `requestId`** — `Ok`, `NoContent`, and `Fail` factory methods now forward the `requestId` parameter to `ApiResponse<T>`.

### Changed
- **`EnvelopeOptions` — new configuration surface** — `CorrelationIdHeader`, `IncludeExceptionDetails`, `JsonSerializerOptions` are now configurable via `AddApiEnvelope(opts => ...)`.
- **Correlation ID propagation** — both `ApiEnvelopeMiddleware` and `EnvelopeResult<T>` read the incoming correlation header and echo it in every response (falls back to `HttpContext.TraceIdentifier`).
- **`EnvelopeOptions` JSON override** — consumers can supply a custom `JsonSerializerOptions` applied to all envelope serialisation.
- **`Envelope.From<T>(response, statusCode)`** — escape hatch for wrapping a manually constructed `ApiResponse<T>` with any HTTP status code.

---

## [1.1.0] — 2025-01-01

### Added
- `requestId` and `timestamp` fields on `ApiResponse<T>` for observability.
- `ApiEnvelopeMiddleware` — global unhandled-exception handler with structured logging.
- `UseApiEnvelope()` extension method.
- `Envelope` static facade covering 200 / 201 / 204 / 400 / 401 / 403 / 404 / 409 / 422 / 429 / 500 / 503.
- `EnvelopeResult<T>` — `IResult` implementation that writes the envelope to the HTTP response.
- `EnvelopeFactory` — domain-layer factory (no ASP.NET Core dependency).
- `PaginationMeta` with `HasMore` computed property.
- `JsonSerializerOptionsProvider` — shared camelCase, compact, nulls-omitted JSON options.

---

## [1.0.0] — 2024-06-01

### Added
- Initial release.
- `ApiResponse<T>` contract with `Success`, `Data`, `Error`, `Meta` fields.
- `ErrorInfo` with `Code`, `Message`, `Details` (field-level validation errors).
- Basic `Envelope.Ok / NotFound / BadRequest` helpers.
