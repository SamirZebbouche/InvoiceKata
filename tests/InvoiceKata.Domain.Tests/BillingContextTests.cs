namespace InvoiceKata.Domain.Tests;

public sealed class BillingContextTests
{
    [Fact]
    public void Should_BeSupported_When_ACompanyBillsAProfessional()
    {
        var context = new BillingContext(IssuerKind.Company, CustomerKind.Professional);

        Assert.True(context.IsSupported);
    }

    // An individual customer, for both kinds of issuer (ADR-0012).
    [Fact]
    public void Should_BeSupported_When_ACompanyBillsAnIndividual()
    {
        var context = new BillingContext(IssuerKind.Company, CustomerKind.Individual);

        Assert.True(context.IsSupported);
    }

    // The second context (ADR-0011).
    [Fact]
    public void Should_BeSupported_When_AnIndividualEntrepreneurBillsAProfessional()
    {
        var context = new BillingContext(IssuerKind.IndividualEntrepreneur, CustomerKind.Professional);

        Assert.True(context.IsSupported);
    }

    [Fact]
    public void Should_BeSupported_When_AnIndividualEntrepreneurBillsAnIndividual()
    {
        var context = new BillingContext(IssuerKind.IndividualEntrepreneur, CustomerKind.Individual);

        Assert.True(context.IsSupported);
    }

    // The world stays closed (ADR-0002): a kind the list does not name, as a future one would be, is refused.
    [Fact]
    public void Should_NotBeSupported_When_TheListDoesNotNameTheContext()
    {
        var context = new BillingContext((IssuerKind)99, CustomerKind.Professional);

        Assert.False(context.IsSupported);
    }
}
