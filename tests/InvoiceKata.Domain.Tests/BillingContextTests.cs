namespace InvoiceKata.Domain.Tests;

public sealed class BillingContextTests
{
    [Fact]
    public void Should_BeSupported_When_ACompanyBillsAProfessional()
    {
        var context = new BillingContext(IssuerKind.Company, CustomerKind.Professional);

        Assert.True(context.IsSupported);
    }

    [Fact]
    public void Should_NotBeSupported_When_ACompanyBillsAnIndividual()
    {
        var context = new BillingContext(IssuerKind.Company, CustomerKind.Individual);

        Assert.False(context.IsSupported);
    }
}
