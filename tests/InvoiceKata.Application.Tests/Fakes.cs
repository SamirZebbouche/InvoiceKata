using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Invoices;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests;

// Fakes of the ports, never of the domain. The stores keep states, as the real adapters do.
internal sealed class StoredQuotes : IQuoteRepository
{
    private readonly Dictionary<Guid, QuoteState> _states = [];

    public Task AddAsync(Quote quote, CancellationToken cancellationToken) => SaveAsync(quote, cancellationToken);

    public Task SaveAsync(Quote quote, CancellationToken cancellationToken)
    {
        _states[quote.Id.Value] = quote.ToState();
        return Task.CompletedTask;
    }

    public Task<Quote> GetAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        Task.FromResult(Quote.FromState(_states[quoteId.Value]));

    public Task<IReadOnlyList<QuoteNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<QuoteNumber>>(
            [.. _states.Values.Where(state => state.IssuerId == issuerId.Value && state.Finalization is not null)
                .Select(state => QuoteNumber.Parse(state.Finalization!.Number))]);
}

internal sealed class StoredInvoices : IInvoiceRepository
{
    private readonly Dictionary<Guid, InvoiceState> _states = [];

    public IReadOnlyCollection<InvoiceState> All => _states.Values;

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        _states.Add(invoice.Id.Value, invoice.ToState());
        return Task.CompletedTask;
    }

    public Task<Invoice> GetAsync(IssuerId issuerId, InvoiceId invoiceId, CancellationToken cancellationToken) =>
        Task.FromResult(Invoice.FromState(_states[invoiceId.Value]));

    public Task<Invoice?> FindByQuoteAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        Task.FromResult(_states.Values.Where(state => state.QuoteId == quoteId.Value).Select(Invoice.FromState).SingleOrDefault());

    public Task<IReadOnlyList<InvoiceNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InvoiceNumber>>(
            [.. _states.Values.Where(state => state.IssuerId == issuerId.Value).Select(state => InvoiceNumber.Parse(state.Number))]);
}

internal sealed class StoredIssuers(params Issuer[] issuers) : IIssuerRepository
{
    public Task AddAsync(Issuer issuer, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Issuer> GetAsync(IssuerId id, CancellationToken cancellationToken) => Task.FromResult(issuers.Single(issuer => issuer.Id == id));
}

internal sealed class StoredCustomers(params Customer[] customers) : ICustomerRepository
{
    public Task AddAsync(Customer customer, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Customer> GetAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken) =>
        Task.FromResult(customers.Single(customer => customer.Id == customerId));
}

internal sealed class StoredPdfs : IPdfStore
{
    private readonly Dictionary<DocumentReference, byte[]> _pdfs = [];

    public Task SaveIfAbsentAsync(IssuerId issuerId, DocumentReference document, byte[] pdf, CancellationToken cancellationToken)
    {
        _pdfs.TryAdd(document, pdf);
        return Task.CompletedTask;
    }

    public Task<byte[]?> FindAsync(IssuerId issuerId, DocumentReference document, CancellationToken cancellationToken) =>
        Task.FromResult(_pdfs.GetValueOrDefault(document));
}

// Renders a document as the bytes of its number, and remembers what it was asked to render.
internal sealed class RecordingRenderer : IDocumentRenderer
{
    public List<ResolvedDocument> Rendered { get; } = [];

    public Task<byte[]> RenderAsync(ResolvedDocument document, CancellationToken cancellationToken)
    {
        Rendered.Add(document);
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"PDF {document.Document.Number} #{Rendered.Count}"));
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
