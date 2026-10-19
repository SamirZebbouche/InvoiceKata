using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

// Secondary port (ADR-0006): the PDF of a document, scoped by issuer (ADR-0007).
public interface IPdfStore
{
    // Stores the PDF unless one is already there: a stored PDF is never written over, so rendering again is harmless.
    Task SaveIfAbsentAsync(IssuerId issuerId, DocumentReference document, byte[] pdf, CancellationToken cancellationToken);

    Task<byte[]?> FindAsync(IssuerId issuerId, DocumentReference document, CancellationToken cancellationToken);
}
