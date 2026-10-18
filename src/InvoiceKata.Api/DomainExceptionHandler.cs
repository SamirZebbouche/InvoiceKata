using InvoiceKata.Domain;

using Microsoft.AspNetCore.Diagnostics;

namespace InvoiceKata.Api;

// The single place where domain failures become HTTP answers, as Problem Details (CDR-0004).
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title)? answer = exception switch
        {
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnsupportedBillingContextException => (StatusCodes.Status422UnprocessableEntity, "Unsupported billing context"),
            _ => null,
        };
        if (answer is not { } known)
        {
            return false;
        }

        httpContext.Response.StatusCode = known.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = known.Status, Title = known.Title, Detail = exception.Message },
        });
    }
}
