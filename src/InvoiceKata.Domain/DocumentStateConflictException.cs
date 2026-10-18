namespace InvoiceKata.Domain;

// The action is forbidden by the state of the document, for instance changing a finalized quote; the API answers 409 (CDR-0004).
public abstract class DocumentStateConflictException(string message) : Exception(message);

public sealed class QuoteAlreadyFinalizedException(QuoteId quoteId)
    : DocumentStateConflictException($"Quote {quoteId.Value} is finalized: it can no longer change.")
{
    public QuoteId QuoteId { get; } = quoteId;
}

public sealed class QuoteWithoutLinesException(QuoteId quoteId)
    : DocumentStateConflictException($"Quote {quoteId.Value} has no line: it cannot be finalized.")
{
    public QuoteId QuoteId { get; } = quoteId;
}
