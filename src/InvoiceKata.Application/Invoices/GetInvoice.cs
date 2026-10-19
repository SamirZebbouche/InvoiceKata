using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices;

public sealed class GetInvoice(IInvoiceRepository invoices)
{
    public Task<Invoice> HandleAsync(IssuerId issuerId, InvoiceId invoiceId, CancellationToken cancellationToken) =>
        invoices.GetAsync(issuerId, invoiceId, cancellationToken);
}
