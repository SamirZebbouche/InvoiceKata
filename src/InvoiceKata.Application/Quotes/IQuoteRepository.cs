using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

// Secondary port (ADR-0006). Every read is scoped by issuer (ADR-0007).
public interface IQuoteRepository
{
    Task AddAsync(Quote quote, CancellationToken cancellationToken);

    Task SaveAsync(Quote quote, CancellationToken cancellationToken);

    /// <exception cref="QuoteNotFoundException">No quote with this identifier belongs to this issuer.</exception>
    Task<Quote> GetAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken);

    // The numbers of this issuer's finalized quotes, from which the domain computes the next one (CDR-0007).
    Task<IReadOnlyList<QuoteNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken);
}
