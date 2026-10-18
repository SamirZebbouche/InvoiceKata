using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class QuoteNotFoundException(IssuerId issuerId, QuoteId quoteId)
    : ResourceNotFoundException($"Quote {quoteId.Value} was not found for issuer {issuerId.Value}.")
{
    public IssuerId IssuerId { get; } = issuerId;

    public QuoteId QuoteId { get; } = quoteId;
}
