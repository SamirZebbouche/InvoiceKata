namespace InvoiceKata.Domain;

// Facts raised by a quote (CDR-0002). The quote only records them; the application dispatches them.
public interface IQuoteEvent;

public sealed record QuoteDrafted(QuoteId QuoteId) : IQuoteEvent;

public sealed record QuoteLineAdded(QuoteId QuoteId, QuoteLineId LineId) : IQuoteEvent;

public sealed record QuoteLineRemoved(QuoteId QuoteId, QuoteLineId LineId) : IQuoteEvent;

// Carries the issuer: a reaction reads the quote through ports that are always scoped by issuer (ADR-0007).
public sealed record QuoteFinalized(QuoteId QuoteId, IssuerId IssuerId, QuoteNumber Number) : IQuoteEvent;
