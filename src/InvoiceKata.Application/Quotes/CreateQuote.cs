using InvoiceKata.Application.Customers;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class CreateQuote(ICustomerRepository customers, IQuoteRepository quotes)
{
    /// <exception cref="CustomerNotFoundException">The customer does not belong to this issuer.</exception>
    public async Task<QuoteId> HandleAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetAsync(issuerId, customerId, cancellationToken);
        var quote = Quote.DraftFor(customer);
        await quotes.AddAsync(quote, cancellationToken);
        return quote.Id;
    }
}
