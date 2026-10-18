using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Acceptance;

// One instance per scenario (Reqnroll context injection): the use cases wired on in-memory adapters.
public sealed class AcceptanceContext
{
    private readonly InMemoryIssuerRepository _issuers = new();
    private readonly InMemoryCustomerRepository _customers = new();

    public ConfigureIssuer ConfigureIssuer => new(_issuers);

    public GetIssuer GetIssuer => new(_issuers);

    public RegisterCustomer RegisterCustomer => new(_issuers, _customers);

    public GetCustomer GetCustomer => new(_customers);

    // Scenarios name issuers ("Batifolio"); the steps resolve those names to identifiers.
    public Dictionary<string, IssuerId> IssuersByName { get; } = [];
}
