using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.Files;

// One directory per issuer: issuers/{issuerId}/issuer.json, customers/{id}.json, quotes/{id}.json.
// Another issuer's data lives in another directory, so a lookup can only find its own (ADR-0007).
internal static class IssuerFolder
{
    public static string Of(IssuerId issuerId) => Path.Combine("issuers", issuerId.Value.ToString());
}

public sealed class FileIssuerRepository(JsonFileStore store) : IIssuerRepository
{
    public Task AddAsync(Issuer issuer, CancellationToken cancellationToken) =>
        store.SaveAsync<Issuer, IssuerState>(PathOf(issuer.Id), issuer, cancellationToken);

    public async Task<Issuer> GetAsync(IssuerId id, CancellationToken cancellationToken) =>
        await store.LoadAsync<Issuer, IssuerState>(PathOf(id), cancellationToken) ?? throw new IssuerNotFoundException(id);

    private static string PathOf(IssuerId id) => Path.Combine(IssuerFolder.Of(id), "issuer.json");
}

public sealed class FileCustomerRepository(JsonFileStore store) : ICustomerRepository
{
    public Task AddAsync(Customer customer, CancellationToken cancellationToken) =>
        store.SaveAsync<Customer, CustomerState>(PathOf(customer.IssuerId, customer.Id), customer, cancellationToken);

    public async Task<Customer> GetAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken) =>
        await store.LoadAsync<Customer, CustomerState>(PathOf(issuerId, customerId), cancellationToken)
        ?? throw new CustomerNotFoundException(issuerId, customerId);

    private static string PathOf(IssuerId issuerId, CustomerId customerId) =>
        Path.Combine(IssuerFolder.Of(issuerId), "customers", $"{customerId.Value}.json");
}

public sealed class FileQuoteRepository(JsonFileStore store) : IQuoteRepository
{
    public Task AddAsync(Quote quote, CancellationToken cancellationToken) => SaveAsync(quote, cancellationToken);

    public Task SaveAsync(Quote quote, CancellationToken cancellationToken) =>
        store.SaveAsync<Quote, QuoteState>(PathOf(quote.IssuerId, quote.Id), quote, cancellationToken);

    public async Task<Quote> GetAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
        await store.LoadAsync<Quote, QuoteState>(PathOf(issuerId, quoteId), cancellationToken)
        ?? throw new QuoteNotFoundException(issuerId, quoteId);

    public async Task<IReadOnlyList<QuoteNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken) =>
        [.. (await store.LoadAllAsync<Quote, QuoteState>(FolderOf(issuerId), cancellationToken))
            .Select(quote => quote.Number)
            .OfType<QuoteNumber>()];

    private static string FolderOf(IssuerId issuerId) => Path.Combine(IssuerFolder.Of(issuerId), "quotes");

    private static string PathOf(IssuerId issuerId, QuoteId quoteId) =>
        Path.Combine(IssuerFolder.Of(issuerId), "quotes", $"{quoteId.Value}.json");
}
