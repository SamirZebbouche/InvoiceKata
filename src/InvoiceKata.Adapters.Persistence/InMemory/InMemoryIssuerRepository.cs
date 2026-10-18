using System.Collections.Concurrent;

using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

// Stores states, not references: like any adapter, it keeps only what was saved (CDR-0006).
public sealed class InMemoryIssuerRepository : IIssuerRepository
{
    private readonly ConcurrentDictionary<Guid, IssuerState> _issuers = new();

    public Task AddAsync(Issuer issuer, CancellationToken cancellationToken)
    {
        _issuers[issuer.Id.Value] = issuer.ToState();
        return Task.CompletedTask;
    }

    public Task<Issuer> GetAsync(IssuerId id, CancellationToken cancellationToken) =>
        _issuers.TryGetValue(id.Value, out var state)
            ? Task.FromResult(Issuer.FromState(state))
            : Task.FromException<Issuer>(new IssuerNotFoundException(id));
}
