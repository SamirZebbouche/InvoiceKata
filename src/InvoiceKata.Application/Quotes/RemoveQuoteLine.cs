using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class RemoveQuoteLine(IQuoteRepository quotes, QuoteScopes scopes, QuoteEventDispatcher events)
{
    /// <exception cref="QuoteNotFoundException">The quote does not belong to this issuer.</exception>
    /// <exception cref="QuoteLineNotFoundException">The line is not on this quote.</exception>
    public async Task HandleAsync(IssuerId issuerId, QuoteId quoteId, QuoteLineId lineId, CancellationToken cancellationToken)
    {
        var quote = await quotes.GetAsync(issuerId, quoteId, cancellationToken);
        quote.RemoveLine(lineId);
        events.DispatchPendingEvents(await scopes.ForAsync(quote, cancellationToken));
        await quotes.SaveAsync(quote, cancellationToken);
    }
}
