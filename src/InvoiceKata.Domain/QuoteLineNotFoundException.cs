namespace InvoiceKata.Domain;

public sealed class QuoteLineNotFoundException(QuoteId quoteId, QuoteLineId lineId)
    : ResourceNotFoundException($"Line {lineId.Value} is not on quote {quoteId.Value}.")
{
    public QuoteId QuoteId { get; } = quoteId;

    public QuoteLineId LineId { get; } = lineId;
}
