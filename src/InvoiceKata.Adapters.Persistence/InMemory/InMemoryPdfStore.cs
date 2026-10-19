using System.Collections.Concurrent;

using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryPdfStore : IPdfStore
{
    private readonly ConcurrentDictionary<(Guid IssuerId, DocumentReference Document), byte[]> _pdfs = new();

    public Task SaveIfAbsentAsync(IssuerId issuerId, DocumentReference document, byte[] pdf, CancellationToken cancellationToken)
    {
        _pdfs.TryAdd((issuerId.Value, document), [.. pdf]);
        return Task.CompletedTask;
    }

    public Task<byte[]?> FindAsync(IssuerId issuerId, DocumentReference document, CancellationToken cancellationToken) =>
        Task.FromResult(_pdfs.TryGetValue((issuerId.Value, document), out var pdf) ? [.. pdf] : (byte[]?)null);
}
