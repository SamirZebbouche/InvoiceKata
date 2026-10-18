namespace InvoiceKata.Domain.Tests;

public sealed class QuoteTotalsTests
{
    [Fact]
    public void Should_RoundTheLineTotalToTheCent_When_QuantityTimesPriceHasMoreDecimals()
    {
        var quote = DraftQuote();

        quote.AddLine(Line(quantity: 18.72m, unitPrice: 33.33m, vatRate: 20m));

        Assert.Equal(new Money(623.94m), quote.Lines.Single().TotalExcludingVat);
    }

    [Fact]
    public void Should_RoundHalfAwayFromZero_When_TheLineTotalFallsOnHalfACent()
    {
        var quote = DraftQuote();

        quote.AddLine(Line(quantity: 1m, unitPrice: 0.125m, vatRate: 20m));

        Assert.Equal(new Money(0.13m), quote.Lines.Single().TotalExcludingVat);
    }

    [Fact]
    public void Should_ComputeTheVatOnTheTotalOfTheRate_When_SeveralLinesShareARate()
    {
        var quote = DraftQuote();

        quote.AddLine(Line(quantity: 1m, unitPrice: 0.03m, vatRate: 20m));
        quote.AddLine(Line(quantity: 1m, unitPrice: 0.03m, vatRate: 20m));

        // 20 % of 0,06 is 0,012, so 0,01; rounding each line first would give 0,02.
        Assert.Equal(new Money(0.01m), quote.TotalVat);
    }

    [Fact]
    public void Should_BreakTheVatDownByRate_When_LinesHaveDifferentRates()
    {
        var quote = DraftQuote();

        quote.AddLine(Line(quantity: 1m, unitPrice: 100m, vatRate: 20m));
        quote.AddLine(Line(quantity: 1m, unitPrice: 50m, vatRate: 10m));

        Assert.Equal(
            [
                new VatBreakdownLine(new VatRate(10m), new Money(50m), new Money(5m)),
                new VatBreakdownLine(new VatRate(20m), new Money(100m), new Money(20m)),
            ],
            quote.VatBreakdown);
        Assert.Equal(new Money(150m), quote.TotalExcludingVat);
        Assert.Equal(new Money(25m), quote.TotalVat);
        Assert.Equal(new Money(175m), quote.TotalIncludingVat);
    }

    private static Quote DraftQuote()
    {
        var issuer = Issuer.ConfigureCompany(
            "Batifolio", LegalForm.Sas, 20000m, new Siren("912345678"), new VatNumber("FR12912345678"),
            new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });
        var customer = Customer.RegisterProfessional(
            issuer.Id, "Les Toits Rouges SARL", new Siren("834567890"), new VatNumber("FR45834567890"),
            new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" });
        return Quote.DraftFor(customer);
    }

    private static NewQuoteLine Line(decimal quantity, decimal unitPrice, decimal vatRate) => new()
    {
        Designation = "Prestation",
        Quantity = new Quantity(quantity),
        Unit = "u",
        UnitPriceExcludingVat = new Money(unitPrice),
        VatRate = new VatRate(vatRate),
    };
}
