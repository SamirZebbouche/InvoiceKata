using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

// Secondary port (ADR-0006): the PDF of a finalized quote, scoped by issuer (ADR-0007).
public interface IPdfStore
{
    // Stores the PDF unless one is already there: a stored PDF is never written over, so rendering again is harmless.
    Task SaveIfAbsentAsync(IssuerId issuerId, QuoteId quoteId, byte[] pdf, CancellationToken cancellationToken);

    Task<byte[]?> FindAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken);
}
