using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class CreateQuote(IIssuerRepository issuers, ICustomerRepository customers, IQuoteRepository quotes)
{
    /// <exception cref="IssuerNotFoundException">The issuer does not exist.</exception>
    /// <exception cref="CustomerNotFoundException">The customer does not belong to this issuer.</exception>
    /// <exception cref="UnsupportedBillingContextException">The product does not handle this issuer and customer pair.</exception>
    public async Task<QuoteId> HandleAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken)
    {
        var issuer = await issuers.GetAsync(issuerId, cancellationToken);
        var customer = await customers.GetAsync(issuerId, customerId, cancellationToken);
        var quote = Quote.DraftFor(issuer, customer);
        await quotes.AddAsync(quote, cancellationToken);
        return quote.Id;
    }
}
