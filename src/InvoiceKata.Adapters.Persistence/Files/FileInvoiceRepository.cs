using InvoiceKata.Application.Invoices;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.Files;

// issuers/{issuerId}/invoices/{invoiceId}.json. An invoice is added once, never written over (ADR-0006): the
// checks and the write share the store's writer lock, so a concurrent issue cannot slip in between (ADR-0010).
public sealed class FileInvoiceRepository(JsonFileStore store) : IInvoiceRepository
{
    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken) =>
        store.SaveAsync<Invoice, InvoiceState>(PathOf(invoice.IssuerId, invoice.Id), invoice, async guarded =>
        {
            if (await store.LoadAsync<Invoice, InvoiceState>(PathOf(invoice.IssuerId, invoice.Id), guarded) is not null)
            {
                throw new InvalidOperationException($"Invoice {invoice.Id.Value} is already stored: an invoice is never written over.");
            }

            var ofIssuer = await store.LoadAllAsync<Invoice, InvoiceState>(FolderOf(invoice.IssuerId), guarded);
            if (ofIssuer.Any(other => other.Number == invoice.Number))
            {
                throw new InvoiceNumberAlreadyTakenException(invoice.IssuerId, invoice.Number);
            }

            if (ofIssuer.Any(other => other.QuoteId == invoice.QuoteId))
            {
                throw new QuoteAlreadyInvoicedException(invoice.QuoteId);
            }
        }, cancellationToken);

    public async Task<Invoice> GetAsync(IssuerId issuerId, InvoiceId invoiceId, CancellationToken cancellationToken) =>
        await store.LoadAsync<Invoice, InvoiceState>(PathOf(issuerId, invoiceId), cancellationToken)
        ?? throw new InvoiceNotFoundException(issuerId, invoiceId);

    public async Task<Invoice?> FindByQuoteAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        (await store.LoadAllAsync<Invoice, InvoiceState>(FolderOf(issuerId), cancellationToken)).SingleOrDefault(invoice => invoice.QuoteId == quoteId);

    public async Task<IReadOnlyList<InvoiceNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken) =>
        [.. (await store.LoadAllAsync<Invoice, InvoiceState>(FolderOf(issuerId), cancellationToken)).Select(invoice => invoice.Number)];

    private static string FolderOf(IssuerId issuerId) => Path.Combine(IssuerFolder.Of(issuerId), "invoices");

    private static string PathOf(IssuerId issuerId, InvoiceId invoiceId) => Path.Combine(FolderOf(issuerId), $"{invoiceId.Value}.json");
}
