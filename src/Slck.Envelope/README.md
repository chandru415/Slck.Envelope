# Slck.Envelope

[![NuGet](https://img.shields.io/nuget/v/Slck.Envelope.svg)](https://www.nuget.org/packages/Slck.Envelope)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Slck.Envelope.svg)](https://www.nuget.org/packages/Slck.Envelope)
[![Build & Publish](https://github.com/chandru415/Slck.Envelope/actions/workflows/main.yml/badge.svg)](https://github.com/chandru415/Slck.Envelope/actions/workflows/main.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Slck.Envelope** is a lightweight, zero-dependency library for ASP.NET Core (Minimal APIs and MVC) that wraps every response in a consistent `ApiResponse<T>` envelope — success, error, pagination, correlation ID, and timestamp included out of the box.

---

## ✨ Features

| Feature | Description |
|---|---|
| Consistent envelope shape | Every response carries `success`, `data`, `error`, `meta`, `requestId`, `timestamp` |
| Minimal API helpers | `Envelope.Ok / Created / Accepted / NotFound / BadRequest / ...` — full HTTP vocabulary |
| MVC action filter | `AddEnvelopeFilter()` wraps controller `ObjectResult` responses automatically |
| Global exception middleware | `UseApiEnvelope()` catches unhandled exceptions and writes a safe envelope |
| Exception → status mapping | Map `NotFoundException` → 404, `ValidationException` → 422, etc. in options — no try/catch in handlers |
| `[SkipEnvelope]` opt-out | Exclude file-download or health-check endpoints from envelope handling |
| Correlation ID propagation | Reads `X-Correlation-ID` from incoming request, echoes it in every response |
| `OnBeforeWriteError` hook | Attach OpenTelemetry `traceId`, tenant ID, or any custom field before error is written |
| `ToResult()` extensions | Bridge `ApiResponse<T>` from your domain/service layer to `IResult` without an HTTP dependency |
| Configurable JSON options | Plug in your own `JsonSerializerOptions`; defaults to camelCase, compact, nulls omitted |
| Stable JSON field order | `[JsonPropertyOrder]` guarantees `success → data → error → meta → requestId → timestamp` |
| Pagination support | `PaginationMeta` with `Page`, `PageSize`, `Total`, `HasMore` (1-based, mathematically correct) |

---

## 📦 Installation

```bash
dotnet add package Slck.Envelope
```

Targets **net9.0** and depends only on `Microsoft.AspNetCore.App` (no extra NuGet packages).

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
  "error": {
    "code": "not_found",
    "message": "Ticket '999' not found"
  },
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

### `ApiResponseExtensions` — bridge from domain layer to `IResult`

```csharp
EnvelopeFactory.Ok(data).ToOkResult();
EnvelopeFactory.Fail<T>("not_found", "...").ToNotFoundResult();
EnvelopeFactory.Ok(job).ToAcceptedResult("/jobs/1/status");
response.ToResult(207); // any status code
```

### `[SkipEnvelope]` — opt-out

```csharp
// Minimal API
app.MapGet("/report/download", ...).WithMetadata(new SkipEnvelopeAttribute());

// MVC
[SkipEnvelope]
public class HealthController : ControllerBase { ... }
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

## 📄 License

MIT — see [LICENSE](LICENSE).
