using InvoiceKata.Application.Invoices;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly Dictionary<Guid, InvoiceState> _invoices = [];

    // Checking and writing under one lock: no concurrent issue can slip in between (ADR-0010).
    private readonly Lock _writer = new();

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var state = invoice.ToState();
        lock (_writer)
        {
            var ofIssuer = _invoices.Values.Where(other => other.IssuerId == state.IssuerId).ToList();
            if (_invoices.ContainsKey(state.Id))
            {
                return Task.FromException(new InvalidOperationException($"Invoice {state.Id} is already stored: an invoice is never written over."));
            }

            if (ofIssuer.Any(other => other.Number == state.Number))
            {
                return Task.FromException(new InvoiceNumberAlreadyTakenException(invoice.IssuerId, invoice.Number));
            }

            if (ofIssuer.Any(other => other.QuoteId == state.QuoteId))
            {
                return Task.FromException(new QuoteAlreadyInvoicedException(invoice.QuoteId));
            }

            _invoices[state.Id] = state;
        }

        return Task.CompletedTask;
    }

    public Task<Invoice> GetAsync(IssuerId issuerId, InvoiceId invoiceId, CancellationToken cancellationToken)
    {
        lock (_writer)
        {
            return _invoices.TryGetValue(invoiceId.Value, out var state) && state.IssuerId == issuerId.Value
                ? Task.FromResult(Invoice.FromState(state))
                : Task.FromException<Invoice>(new InvoiceNotFoundException(issuerId, invoiceId));
        }
    }

    public Task<Invoice?> FindByQuoteAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken)
    {
        lock (_writer)
        {
            var state = _invoices.Values.SingleOrDefault(other => other.IssuerId == issuerId.Value && other.QuoteId == quoteId.Value);
            return Task.FromResult(state is null ? null : Invoice.FromState(state));
        }
    }

    public Task<IReadOnlyList<InvoiceNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken)
    {
        lock (_writer)
        {
            return Task.FromResult<IReadOnlyList<InvoiceNumber>>(
                [.. _invoices.Values.Where(state => state.IssuerId == issuerId.Value).Select(state => InvoiceNumber.Parse(state.Number))]);
        }
    }
}
