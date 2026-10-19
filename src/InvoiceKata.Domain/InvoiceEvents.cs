namespace InvoiceKata.Domain;

// Facts raised by an invoice. The invoice only records them; the application publishes them after the save.
public interface IInvoiceEvent;

// Carries the issuer: a reaction reads the invoice through ports that are always scoped by issuer (ADR-0007).
public sealed record InvoiceIssued(InvoiceId InvoiceId, IssuerId IssuerId, InvoiceNumber Number) : IInvoiceEvent;
