using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Invoices;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

// Renders the PDF of an issued invoice, once: as a reaction to InvoiceIssued, and again when the PDF is read but
// missing, for instance because the reaction failed (ADR-0010). A stored PDF is never rendered again.
public sealed class RenderInvoicePdf(
    IInvoiceRepository invoices, IIssuerRepository issuers, ICustomerRepository customers, FinalDocumentValidator validator, IDocumentRenderer renderer, IPdfStore pdfs)
    : IInvoiceReaction
{
    public async Task ReactAsync(IInvoiceEvent invoiceEvent, CancellationToken cancellationToken)
    {
        if (invoiceEvent is InvoiceIssued issued)
        {
            await PdfOfAsync(issued.IssuerId, issued.InvoiceId, cancellationToken);
        }
    }

    /// <exception cref="InvoiceNotFoundException">The invoice does not belong to this issuer.</exception>
    /// <exception cref="InvalidDocumentException">The final validator refuses the document: no PDF is rendered.</exception>
    public async Task<byte[]> PdfOfAsync(IssuerId issuerId, InvoiceId invoiceId, CancellationToken cancellationToken)
    {
        var reference = DocumentReference.Of(invoiceId);
        if (await pdfs.FindAsync(issuerId, reference, cancellationToken) is { } stored)
        {
            return stored;
        }

        var invoice = await invoices.GetAsync(issuerId, invoiceId, cancellationToken);
        var document = ResolvedDocument.OfInvoice(
            invoice,
            await issuers.GetAsync(issuerId, cancellationToken),
            await customers.GetAsync(issuerId, invoice.CustomerId, cancellationToken));
        validator.Validate(document);
        await pdfs.SaveIfAbsentAsync(issuerId, reference, await renderer.RenderAsync(document, cancellationToken), cancellationToken);
        return (await pdfs.FindAsync(issuerId, reference, cancellationToken))!;
    }
}
