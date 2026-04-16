using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Slck.Envelope.Contracts;

namespace Slck.Envelope.AspNetCore
{
    /// <summary>
    /// MVC action filter that wraps bare <see cref="ObjectResult"/> responses from controllers
    /// in the standard <see cref="ApiResponse{T}"/> envelope.
    ///
    /// Register globally via:
    /// <code>
    /// builder.Services.AddControllers()
    ///                 .AddEnvelopeFilter();
    /// </code>
    ///
    /// Decorate individual controllers or actions with <see cref="SkipEnvelopeAttribute"/>
    /// to bypass the filter for specific endpoints.
    ///
    /// The filter intentionally does NOT re-wrap responses that are already an
    /// <see cref="ApiResponse{T}"/> so it is safe to mix Envelope-aware and legacy
    /// controller actions in the same application.
    /// </summary>
    public sealed class EnvelopeActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Opt-out: controller class or action method is decorated with [SkipEnvelope]
            if (context.ActionDescriptor.EndpointMetadata.OfType<ISkipEnvelopeMetadata>().Any())
                return;

            switch (context.Result)
            {
                // 200 OK with a body — wrap the value
                case OkObjectResult ok:
                    if (!IsAlreadyEnveloped(ok.Value))
                        context.Result = new OkObjectResult(WrapOk(ok.Value));
                    break;

                // 201 Created — wrap the value; preserve Location
                case CreatedResult created:
                    if (!IsAlreadyEnveloped(created.Value))
                        context.Result = new CreatedResult(created.Location ?? string.Empty, WrapOk(created.Value));
                    break;

                case CreatedAtActionResult createdAtAction:
                    if (!IsAlreadyEnveloped(createdAtAction.Value))
                        createdAtAction.Value = WrapOk(createdAtAction.Value);
                    break;

                case CreatedAtRouteResult createdAtRoute:
                    if (!IsAlreadyEnveloped(createdAtRoute.Value))
                        createdAtRoute.Value = WrapOk(createdAtRoute.Value);
                    break;

                // 204 No Content — leave as-is; no body expected
                case NoContentResult:
                    break;

                // 400 / 422 validation → map ModelState errors into ErrorInfo.Details
                case BadRequestObjectResult { Value: Microsoft.AspNetCore.Mvc.SerializableError errors }:
                    context.Result = new BadRequestObjectResult(
                        ApiResponse<object>.Fail("bad_request", "Validation failed", ToDetails(errors)));
                    break;

                // Any other 4xx/5xx ObjectResult — wrap with generic envelope
                case ObjectResult obj when obj.StatusCode >= 400:
                    if (!IsAlreadyEnveloped(obj.Value))
                        obj.Value = ApiResponse<object>.Fail(
                            StatusCodeToErrorCode(obj.StatusCode ?? 500),
                            obj.Value?.ToString() ?? "An error occurred");
                    break;
            }
        }

        private static object WrapOk(object? value) =>
            ApiResponse<object>.Ok(value!);

        private static bool IsAlreadyEnveloped(object? value) =>
            value is not null &&
            value.GetType() is { IsGenericType: true } t &&
            t.GetGenericTypeDefinition() == typeof(ApiResponse<>);

        private static IDictionary<string, string[]?> ToDetails(Microsoft.AspNetCore.Mvc.SerializableError errors) =>
            errors.ToDictionary(
                kvp => kvp.Key,
                kvp => (string[]?)(kvp.Value is string[] arr ? arr : new[] { kvp.Value?.ToString() ?? string.Empty }));

        private static string StatusCodeToErrorCode(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest      => "bad_request",
            StatusCodes.Status401Unauthorized    => "unauthorized",
            StatusCodes.Status403Forbidden       => "forbidden",
            StatusCodes.Status404NotFound        => "not_found",
            StatusCodes.Status409Conflict        => "conflict",
            StatusCodes.Status422UnprocessableEntity => "validation_error",
            StatusCodes.Status429TooManyRequests => "too_many_requests",
            StatusCodes.Status503ServiceUnavailable => "service_unavailable",
            _ => "server_error"
        };
    }
}
