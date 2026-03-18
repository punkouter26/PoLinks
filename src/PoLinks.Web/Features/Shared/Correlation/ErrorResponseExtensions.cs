// T011: Structured error response helpers (FR-017).
// Maps exceptions to RFC-7807 ProblemDetails with correlation IDs included.
namespace PoLinks.Web.Features.Shared.Correlation;

/// <summary>
/// Extension methods for wiring up consistent ProblemDetails error responses
/// including the CorrelationId field so clients can include it in support tickets.
/// </summary>
public static class ErrorResponseExtensions
{
    /// <summary>
    /// Adds a standard ProblemDetails response body that includes the current
    /// correlation ID drawn from <see cref="HttpContext.TraceIdentifier"/>.
    /// </summary>
    public static void AddCorrelatedProblemDetails(
        this WebApplication app,
        bool includeExceptionDetails = false)
    {
        app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Detail = includeExceptionDetails
                        ? context.Features
                            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()
                            ?.Error.Message
                        : null,
                };
                problem.Extensions["correlationId"] = context.TraceIdentifier;

                await context.Response.WriteAsJsonAsync(problem);
            });
        });
    }
}
