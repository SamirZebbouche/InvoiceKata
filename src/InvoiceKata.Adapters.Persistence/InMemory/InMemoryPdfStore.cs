using System.Collections.Concurrent;

using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryPdfStore : IPdfStore
{
    private readonly ConcurrentDictionary<(Guid IssuerId, Guid QuoteId), byte[]> _pdfs = new();

    public Task SaveIfAbsentAsync(IssuerId issuerId, QuoteId quoteId, byte[] pdf, CancellationToken cancellationToken)
    {
        _pdfs.TryAdd((issuerId.Value, quoteId.Value), [.. pdf]);
        return Task.CompletedTask;
    }

    public Task<byte[]?> FindAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        Task.FromResult(_pdfs.TryGetValue((issuerId.Value, quoteId.Value), out var pdf) ? [.. pdf] : (byte[]?)null);
}
