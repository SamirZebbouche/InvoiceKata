using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Diagnostics;

namespace InvoiceKata.Api;

// The single place where domain failures become HTTP answers, as Problem Details (CDR-0004).
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    // Every exception of the product must have an answer here; a test walks them all. Anything else is a bug: 500.
    public static int? StatusFor(Exception exception) => exception switch
    {
        ResourceNotFoundException => StatusCodes.Status404NotFound,
        UnsupportedBillingContextException => StatusCodes.Status422UnprocessableEntity,
        VatRateNotApplicableException => StatusCodes.Status422UnprocessableEntity,
        DocumentStateConflictException => StatusCodes.Status409Conflict,
        // A defect of the product, answered explicitly: the product refuses to render a wrong document (ADR-0005).
        InvalidDocumentException => StatusCodes.Status500InternalServerError,
        _ => null,
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (StatusFor(exception) is not { } status)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Detail = exception.Message },
        });
    }
}
