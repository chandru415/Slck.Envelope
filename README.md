<p align="center">
 <h2 align="center">Slck.Envelope</h2>
 <p align="center">A lightweight, opinionated library for ASP.NET Core minimal APIs that standardizes your response body <br/>
 It provides a consistent envelope (`ApiResponse<T>`), `IResult` wrappers for common HTTP status codes, and middleware to catch unhandled exceptions.</p>
 <br/>
 <p align="center">
 <img src="https://img.shields.io/github/stars/chandru415/Slck.Envelope?style=for-the-badge" />
 <img src="https://img.shields.io/github/watchers/chandru415/Slck.Envelope?style=for-the-badge" />
  <a href="https://www.nuget.org/packages/Slck.Envelope/">
   <img src="https://img.shields.io/nuget/dt/Slck.Envelope?style=for-the-badge" />
 </a>
 </p>
</p>
<br/>



# :flashlight: Slck.Envelope

**Slck.Envelope** is a lightweight, opinionated library for ASP.NET Core minimal APIs that standardizes API responses into a consistent envelope format. It provides a structured way to handle success responses, errors, and metadata across your entire API.

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
