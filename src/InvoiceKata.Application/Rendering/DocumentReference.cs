using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

public enum DocumentKind
{
    Quote,
    Invoice,
}

// Which document a PDF renders: the PDF store keeps the PDF of any kind of document (ADR-0010).
public readonly record struct DocumentReference(DocumentKind Kind, Guid Id)
{
    public static DocumentReference Of(QuoteId quoteId) => new(DocumentKind.Quote, quoteId.Value);

    public static DocumentReference Of(InvoiceId invoiceId) => new(DocumentKind.Invoice, invoiceId.Value);
}
