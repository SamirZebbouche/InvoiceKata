using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class AddQuoteLine(IQuoteRepository quotes)
{
    /// <exception cref="QuoteNotFoundException">The quote does not belong to this issuer.</exception>
    public async Task<QuoteLineId> HandleAsync(IssuerId issuerId, QuoteId quoteId, NewQuoteLine line, CancellationToken cancellationToken)
    {
        var quote = await quotes.GetAsync(issuerId, quoteId, cancellationToken);
        var lineId = quote.AddLine(line);
        await quotes.SaveAsync(quote, cancellationToken);
        return lineId;
    }
}
