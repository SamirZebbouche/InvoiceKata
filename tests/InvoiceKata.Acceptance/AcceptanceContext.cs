using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Acceptance;

// One instance per scenario (Reqnroll context injection): the use cases wired on in-memory adapters.
public sealed class AcceptanceContext
{
    private readonly InMemoryIssuerRepository _issuers = new();
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryQuoteRepository _quotes = new();
    private readonly QuoteEventDispatcher _events;
    private readonly QuoteScopes _scopes;

    public AcceptanceContext()
    {
        var texts = new FrenchMentionTexts();
        _events = new QuoteEventDispatcher([new IssuerLegalIdentityMention(texts)]);
        _scopes = new QuoteScopes(_issuers, _customers);
    }

    public ConfigureIssuer ConfigureIssuer => new(_issuers);

    public GetIssuer GetIssuer => new(_issuers);

    public RegisterCustomer RegisterCustomer => new(_issuers, _customers);

    public RegisterIndividualCustomer RegisterIndividualCustomer => new(_issuers, _customers);

    public GetCustomer GetCustomer => new(_customers);

    public CreateQuote CreateQuote => new(_issuers, _customers, _quotes, _events);

    public GetQuote GetQuote => new(_quotes);

    public AddQuoteLine AddQuoteLine => new(_quotes, _scopes, _events);

    public RemoveQuoteLine RemoveQuoteLine => new(_quotes, _scopes, _events);

    // Scenarios name issuers ("Batifolio"); the steps resolve those names to identifiers.
    public Dictionary<string, IssuerId> IssuersByName { get; } = [];

    public Dictionary<string, CustomerId> CustomersByName { get; } = [];

    // The failure of the last "When" step, for the "Then … introuvable" steps.
    public Exception? LastFailure { get; set; }
}
