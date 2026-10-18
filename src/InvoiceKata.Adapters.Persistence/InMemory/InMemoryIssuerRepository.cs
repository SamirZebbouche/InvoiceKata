using System.Collections.Concurrent;

using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryIssuerRepository : IIssuerRepository
{
    private readonly ConcurrentDictionary<IssuerId, Issuer> _issuers = new();

    public Task AddAsync(Issuer issuer, CancellationToken cancellationToken)
    {
        _issuers[issuer.Id] = issuer;
        return Task.CompletedTask;
    }

    public Task<Issuer> GetAsync(IssuerId id, CancellationToken cancellationToken) =>
        _issuers.TryGetValue(id, out var issuer)
            ? Task.FromResult(issuer)
            : Task.FromException<Issuer>(new IssuerNotFoundException(id));
}
