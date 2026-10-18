using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class GetQuote(IQuoteRepository quotes)
{
    public Task<Quote> HandleAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        quotes.GetAsync(issuerId, quoteId, cancellationToken);
}
