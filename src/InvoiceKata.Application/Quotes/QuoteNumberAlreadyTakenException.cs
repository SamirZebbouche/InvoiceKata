using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

// Raised by the port when a concurrent finalization took the number first (CDR-0007); the client can finalize again.
public sealed class QuoteNumberAlreadyTakenException(IssuerId issuerId, QuoteNumber number)
    : DocumentStateConflictException($"Number {number} is already taken by another quote of issuer {issuerId.Value}.")
{
    public IssuerId IssuerId { get; } = issuerId;

    public QuoteNumber Number { get; } = number;
}
