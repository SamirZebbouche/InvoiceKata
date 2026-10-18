using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class QuoteScopes(IIssuerRepository issuers, ICustomerRepository customers)
{
    public async Task<QuoteScope> ForAsync(Quote quote, CancellationToken cancellationToken) => new(
        quote,
        await issuers.GetAsync(quote.IssuerId, cancellationToken),
        await customers.GetAsync(quote.IssuerId, quote.CustomerId, cancellationToken));
}
