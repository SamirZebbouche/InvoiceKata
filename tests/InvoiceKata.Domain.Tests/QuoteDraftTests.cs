namespace InvoiceKata.Domain.Tests;

public sealed class QuoteDraftTests
{
    [Fact]
    public void Should_RefuseToDraftAQuote_When_TheCustomerBelongsToAnotherIssuer()
    {
        var customerOfAnotherIssuer = Quotes.ProfessionalCustomerOf(Quotes.Issuer());

        Assert.Throws<ArgumentException>(() => Quote.DraftFor(Quotes.Issuer(), customerOfAnotherIssuer));
    }

    [Fact]
    public void Should_BelongToTheIssuerAndTheCustomer_When_Drafted()
    {
        var issuer = Quotes.Issuer();
        var customer = Quotes.ProfessionalCustomerOf(issuer);

        var quote = Quote.DraftFor(issuer, customer);

        Assert.Equal(issuer.Id, quote.IssuerId);
        Assert.Equal(customer.Id, quote.CustomerId);
        Assert.Equal(QuoteStatus.Draft, quote.Status);
    }
}
