using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices;

// Secondary port (ADR-0006). Every read is scoped by issuer (ADR-0007). An invoice is added once and never
// written over: there is no save.
public interface IInvoiceRepository
{
    /// <exception cref="InvoiceNumberAlreadyTakenException">Another invoice of the issuer has this number.</exception>
    /// <exception cref="QuoteAlreadyInvoicedException">The quote already has its invoice.</exception>
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    /// <exception cref="InvoiceNotFoundException">No invoice with this identifier belongs to this issuer.</exception>
    Task<Invoice> GetAsync(IssuerId issuerId, InvoiceId invoiceId, CancellationToken cancellationToken);

    Task<Invoice?> FindByQuoteAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken);

    // The numbers of this issuer's invoices, from which the domain computes the next one (ADR-0010).
    Task<IReadOnlyList<InvoiceNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken);
}
