namespace InvoiceKata.Domain.Tests;

public sealed class QuoteDraftTests
{
    [Fact]
    public void Should_RefuseToDraftAQuote_When_TheBillingContextIsNotSupported()
    {
        var issuer = Quotes.Issuer();
        var individual = Customer.RegisterIndividual(
            issuer.Id, "Jeanne Martin", new Address { Street = "3 rue des Lilas", PostalCode = "33000", City = "Bordeaux" });

        var refusal = Assert.Throws<UnsupportedBillingContextException>(() => Quote.DraftFor(issuer, individual));

        Assert.Equal(new BillingContext(IssuerKind.Company, CustomerKind.Individual), refusal.Context);
    }

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
