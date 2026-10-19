using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices;

public sealed class InvoiceNotFoundException(IssuerId issuerId, InvoiceId invoiceId)
    : ResourceNotFoundException($"Invoice {invoiceId.Value} was not found for issuer {issuerId.Value}.")
{
    public IssuerId IssuerId { get; } = issuerId;

    public InvoiceId InvoiceId { get; } = invoiceId;
}

// Raised by the port when a concurrent issue took the number first (ADR-0010); the client can issue again.
public sealed class InvoiceNumberAlreadyTakenException(IssuerId issuerId, InvoiceNumber number)
    : DocumentStateConflictException($"Number {number} is already taken by another invoice of issuer {issuerId.Value}.")
{
    public IssuerId IssuerId { get; } = issuerId;

    public InvoiceNumber Number { get; } = number;
}

// A quote gives at most one invoice (ADR-0010).
public sealed class QuoteAlreadyInvoicedException(QuoteId quoteId)
    : DocumentStateConflictException($"Quote {quoteId.Value} already has its invoice.")
{
    public QuoteId QuoteId { get; } = quoteId;
}
