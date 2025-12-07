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



# Slck.Envelope

**Slck.Envelope** is a lightweight, opinionated library for ASP.NET Core minimal APIs that standardizes your response body.  
It provides a consistent envelope (`ApiResponse<T>`), `IResult` wrappers for common HTTP status codes, and middleware to catch unhandled exceptions.

---

## ? Features

- **Consistent response shape**: Every endpoint returns `ApiResponse<T>` with `success`, `data`, `error`, and optional `meta`.
- **Minimal API integration**: Use `Envelope.Ok(...)`, `Envelope.NotFound(...)`, etc. directly in your handlers.
- **Middleware support**: Automatically wraps unhandled exceptions into a standardized error envelope.
- **Pagination metadata**: Built?in `PaginationMeta` for list endpoints.
- **Developer friendly**: Simple factories, extension helpers, and JSON options (camelCase, ignore nulls).

---

## ?? Installation

Add the package from NuGet (or your local feed):

```bash
dotnet add package Slck.Envelope
