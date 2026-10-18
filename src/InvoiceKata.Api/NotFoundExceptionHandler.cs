using InvoiceKata.Application;

using Microsoft.AspNetCore.Diagnostics;

namespace InvoiceKata.Api;

// Top-level translation of "not found" use-case failures into 404 Problem Details.
public sealed class NotFoundExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ResourceNotFoundException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = StatusCodes.Status404NotFound, Title = "Resource not found" },
        });
    }
}
