using System.Collections.Concurrent;

using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryQuoteRepository : IQuoteRepository
{
    private readonly ConcurrentDictionary<Guid, QuoteState> _quotes = new();

    public Task AddAsync(Quote quote, CancellationToken cancellationToken) => SaveAsync(quote, cancellationToken);

    public Task SaveAsync(Quote quote, CancellationToken cancellationToken)
    {
        _quotes[quote.Id.Value] = quote.ToState();
        return Task.CompletedTask;
    }

    public Task<Quote> GetAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        _quotes.TryGetValue(quoteId.Value, out var state) && state.IssuerId == issuerId.Value
            ? Task.FromResult(Quote.FromState(state))
            : Task.FromException<Quote>(new QuoteNotFoundException(issuerId, quoteId));

    public Task<IReadOnlyList<QuoteNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<QuoteNumber>>(
            [.. _quotes.Values
                .Where(state => state.IssuerId == issuerId.Value && state.Finalization is not null)
                .Select(state => QuoteNumber.Parse(state.Finalization!.Number))]);
}
