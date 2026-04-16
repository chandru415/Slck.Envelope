<p align="center">
 <h2 align="center">Slck.Envelope</h2>
 <p align="center">A lightweight, zero-dependency library for ASP.NET Core (Minimal APIs &amp; MVC) that wraps every response<br/>
 in a consistent <code>ApiResponse&lt;T&gt;</code> envelope — success, error, pagination, correlation ID, and timestamp included.</p>
 <br/>
 <p align="center">
  <img src="https://img.shields.io/github/stars/chandru415/Slck.Envelope?style=for-the-badge" />
  <img src="https://img.shields.io/github/watchers/chandru415/Slck.Envelope?style=for-the-badge" />
  <a href="https://www.nuget.org/packages/Slck.Envelope/">
   <img src="https://img.shields.io/nuget/v/Slck.Envelope?style=for-the-badge" />
  </a>
  <a href="https://www.nuget.org/packages/Slck.Envelope/">
   <img src="https://img.shields.io/nuget/dt/Slck.Envelope?style=for-the-badge" />
  </a>
  <a href="https://github.com/chandru415/Slck.Envelope/actions/workflows/main.yml">
   <img src="https://img.shields.io/github/actions/workflow/status/chandru415/Slck.Envelope/main.yml?style=for-the-badge&label=CI" />
  </a>
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge" />
 </p>
</p>
<br/>

---

## ✨ Features

| Feature | Description |
|---|---|
| Consistent envelope shape | Every response carries `success`, `data`, `error`, `meta`, `requestId`, `timestamp` |
| Minimal API helpers | `Envelope.Ok / Created / Accepted / NotFound / BadRequest / ...` — full HTTP vocabulary |
| MVC action filter | `AddEnvelopeFilter()` wraps controller `ObjectResult` responses automatically |
| Global exception middleware | `UseApiEnvelope()` catches unhandled exceptions and writes a safe envelope |
| Exception → status mapping | Map `NotFoundException` → 404, `ValidationException` → 422 in options — no try/catch in handlers |
| `[SkipEnvelope]` opt-out | Exclude file-download or health-check endpoints from envelope handling |
| Correlation ID propagation | Reads `X-Correlation-ID` from request, echoes it in every response |
| `OnBeforeWriteError` hook | Attach OpenTelemetry `traceId`, tenant ID, or any custom field before an error is written |
| `ToResult()` extensions | Bridge `ApiResponse<T>` from your domain layer to `IResult` without an HTTP dependency |
| Configurable JSON options | Plug in your own `JsonSerializerOptions`; defaults to camelCase, compact, nulls omitted |
| Stable JSON field order | `[JsonPropertyOrder]` guarantees `success → data → error → meta → requestId → timestamp` |
| Pagination support | `PaginationMeta` with `Page`, `PageSize`, `Total`, `HasMore` |
| Multi-target | Ships `net9.0` and `net10.0` assemblies in one package |

---

## 📦 Installation

```bash
dotnet add package Slck.Envelope
```

---

## 🚀 Quick Start

### 1. Register (optional — all defaults are safe)

```csharp
builder.Services.AddApiEnvelope(opts =>
{
    opts.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    opts.CorrelationIdHeader = "X-Correlation-ID"; // default

    // Map domain exceptions to HTTP status codes — no try/catch in handlers
    opts.MapException<KeyNotFoundException>(404, "not_found")
        .MapException<UnauthorizedAccessException>(401, "unauthorized");

    // Attach OpenTelemetry trace ID to every error envelope
    opts.OnBeforeWriteError = (ex, response) =>
        response with { RequestId = Activity.Current?.TraceId.ToString() ?? response.RequestId };
});
```

### 2. Add middleware + (optional) MVC filter

```csharp
app.UseApiEnvelope(); // global unhandled-exception handler

// For MVC controllers:
builder.Services.AddControllers().AddEnvelopeFilter();
```

### 3. Return envelopes from Minimal API endpoints

```csharp
app.MapGet("/ticket/{id}", (string id) =>
{
    var ticket = db.Find(id);
    return ticket is null
        ? Envelope.NotFound($"Ticket '{id}' not found")
        : Envelope.Ok(ticket);
});

app.MapGet("/tickets", (int page = 1, int pageSize = 10) =>
{
    var meta = new PaginationMeta { Page = page, PageSize = pageSize, Total = total };
    return Envelope.Ok(paged, meta);
});

app.MapPost("/ticket", (Ticket t) => Envelope.Created($"/ticket/{t.Id}", t));

// 202 Accepted — async job pattern
app.MapPost("/jobs", (Job j) => Envelope.Accepted(new { j.Id, Status = "queued" }, $"/jobs/{j.Id}/status"));
```

---

## 📐 Response Shape

### Success — `200 OK`
```json
{
  "success": true,
  "data": { "id": "123", "title": "Sample Ticket" },
  "meta": { "page": 1, "pageSize": 10, "total": 3, "hasMore": false },
  "requestId": "0HNK...",
  "timestamp": "2026-04-16T08:00:00+00:00"
}
```

### Error — `404 Not Found`
```json
{
  "success": false,
  "error": { "code": "not_found", "message": "Ticket '999' not found" },
  "requestId": "0HNK...",
  "timestamp": "2026-04-16T08:00:01+00:00"
}
```

### Validation Error — `422 Unprocessable Entity`
```json
{
  "success": false,
  "error": {
    "code": "validation_error",
    "message": "Validation failed",
    "details": {
      "id":    ["Id is required."],
      "title": ["Title is required."]
    }
  },
  "requestId": "0HNK...",
  "timestamp": "2026-04-16T08:00:02+00:00"
}
```

---

## 🛠️ Full API Reference

### `Envelope` — Minimal API static helpers

| Method | Status |
|---|---|
| `Envelope.Ok(data, meta?)` | 200 |
| `Envelope.Created(location, data)` | 201 + `Location` header |
| `Envelope.Accepted(data, statusUrl?)` | 202 + `Location` header |
| `Envelope.NoContent()` | 204 |
| `Envelope.BadRequest(message, details?)` | 400 |
| `Envelope.Unauthorized(message)` | 401 |
| `Envelope.Forbidden(message)` | 403 |
| `Envelope.NotFound(message)` | 404 |
| `Envelope.Conflict(message)` | 409 |
| `Envelope.UnprocessableEntity(errors, message)` | 422 |
| `Envelope.TooManyRequests(message)` | 429 |
| `Envelope.Error(message, code)` | 500 |
| `Envelope.ServiceUnavailable(message)` | 503 |
| `Envelope.From(response, statusCode)` | any (escape hatch) |

### `[SkipEnvelope]` — opt-out

```csharp
// Minimal API
app.MapGet("/report/download", ...).WithMetadata(new SkipEnvelopeAttribute());

// MVC
[SkipEnvelope]
public class HealthController : ControllerBase { ... }
```

### `ApiResponseExtensions` — domain layer → `IResult`

```csharp
EnvelopeFactory.Ok(data).ToOkResult();
EnvelopeFactory.Fail<T>("not_found", "...").ToNotFoundResult();
EnvelopeFactory.Ok(job).ToAcceptedResult("/jobs/1/status");
response.ToResult(207); // any status code
```

---

## ⚙️ `EnvelopeOptions`

| Property | Default | Description |
|---|---|---|
| `IncludeExceptionDetails` | `false` | Include `ex.Message` in 500 responses (dev only) |
| `CorrelationIdHeader` | `"X-Correlation-ID"` | Request/response correlation header |
| `JsonSerializerOptions` | `null` | Override JSON serialization globally |
| `ExceptionMap` | `{}` | Dictionary of `Type → ExceptionMapping` |
| `OnBeforeWriteError` | `null` | Hook to enrich error envelopes before write |

---

## 🗂️ Project Structure

```
src/Slck.Envelope/     — library source
samples/sample.api/    — runnable sample demonstrating all features
CHANGELOG.md           — full version history
```

---

## 📄 License

MIT — see [LICENSE](src/Slck.Envelope/LICENSE).




# :flashlight: Slck.Envelope

**Slck.Envelope** is a lightweight, opinionated library for ASP.NET Core minimal APIs that standardizes API responses into a consistent envelope format. It provides a structured way to handle success responses, errors, and metadata across your entire API.

---

## Project Structure

- `src/Slck.Envelope` - package source
- `samples/sample.api` - active sample API in the solution
- `tests` - place future test projects here

---

## ✨ Features

- **Consistent Response Shape**: Every endpoint returns `ApiResponse<T>` with standardized properties:
  - `success`: Boolean indicating request status
  - `data`: The response payload (null on error)
  - `error`: Error details (null on success)
  - `meta`: Optional metadata (pagination, timestamps, etc.)

- **Minimal API Integration**: Seamless integration with ASP.NET Core minimal APIs using `IResult` implementations:
  - `Envelope.Ok(data, message)`: 200 OK responses
  - `Envelope.Created(data, uri)`: 201 Created responses
  - `Envelope.NotFound(message)`: 404 Not Found responses
  - `Envelope.BadRequest(message, errors)`: 400 Bad Request with validation errors
  - `Envelope.Unauthorized(message)`: 401 Unauthorized responses
  - `Envelope.InternalServerError(message)`: 500 Internal Server Error

- **Exception Handling Middleware**: Automatic wrapping of unhandled exceptions into standardized error envelopes.

- **Pagination Support**: Built-in `PaginationMeta` for paginated list endpoints with:
  - Page number
  - Page size
  - Total items
  - Total pages
  - Navigation links

- **Developer Experience**:
  - CamelCase JSON serialization
  - Ignore null properties
  - Extension methods for common scenarios
  - Type-safe response factories

---

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package Slck.Envelope
