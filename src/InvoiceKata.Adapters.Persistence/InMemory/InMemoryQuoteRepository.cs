using System.Collections.Concurrent;

using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryQuoteRepository : IQuoteRepository
{
    private readonly ConcurrentDictionary<Guid, QuoteState> _quotes = new();

    // Checking and writing under one lock: no concurrent save can slip in between (CDR-0007).
    private readonly Lock _writer = new();

    public Task AddAsync(Quote quote, CancellationToken cancellationToken) => SaveAsync(quote, cancellationToken);

    /// <exception cref="QuoteAlreadyFinalizedException">The stored quote is finalized: a frozen document is never written over.</exception>
    /// <exception cref="QuoteNumberAlreadyTakenException">Another quote of the issuer has this number.</exception>
    public Task SaveAsync(Quote quote, CancellationToken cancellationToken)
    {
        var state = quote.ToState();
        lock (_writer)
        {
            if (_quotes.TryGetValue(state.Id, out var stored) && stored.Finalization is not null)
            {
                return Task.FromException(new QuoteAlreadyFinalizedException(quote.Id));
            }

            if (state.Finalization is { } finalization && _quotes.Values.Any(other =>
                other.Id != state.Id && other.IssuerId == state.IssuerId && other.Finalization?.Number == finalization.Number))
            {
                return Task.FromException(new QuoteNumberAlreadyTakenException(quote.IssuerId, quote.Number!.Value));
            }

            _quotes[state.Id] = state;
        }

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
