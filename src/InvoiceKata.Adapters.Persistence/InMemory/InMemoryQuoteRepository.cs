using System.Collections.Concurrent;

using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryQuoteRepository : IQuoteRepository
{
    private readonly ConcurrentDictionary<QuoteId, Quote> _quotes = new();

    public Task AddAsync(Quote quote, CancellationToken cancellationToken)
    {
        _quotes[quote.Id] = quote;
        return Task.CompletedTask;
    }

    public Task<Quote> GetAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        _quotes.TryGetValue(quoteId, out var quote) && quote.IssuerId == issuerId
            ? Task.FromResult(quote)
            : Task.FromException<Quote>(new QuoteNotFoundException(issuerId, quoteId));
}
