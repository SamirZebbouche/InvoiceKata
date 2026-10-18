using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Application.Issuers;

namespace InvoiceKata.Acceptance;

// One instance per scenario (Reqnroll context injection): the use cases wired on in-memory adapters.
public sealed class AcceptanceContext
{
    private readonly InMemoryIssuerRepository _issuers = new();

    public ConfigureIssuer ConfigureIssuer => new(_issuers);

    public GetIssuer GetIssuer => new(_issuers);
}
