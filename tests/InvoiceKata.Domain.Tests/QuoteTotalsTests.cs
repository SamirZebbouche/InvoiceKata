namespace InvoiceKata.Domain.Tests;

public sealed class QuoteTotalsTests
{
    [Fact]
    public void Should_RoundTheLineTotalToTheCent_When_QuantityTimesPriceHasMoreDecimals()
    {
        var quote = Quotes.Draft();

        quote.AddLine(Quotes.Line(unitPrice: 33.33m, quantity: 18.72m, vatRate: 20m));

        Assert.Equal(new Money(623.94m), quote.Lines.Single().TotalExcludingVat);
    }

    [Fact]
    public void Should_RoundHalfAwayFromZero_When_TheLineTotalFallsOnHalfACent()
    {
        var quote = Quotes.Draft();

        quote.AddLine(Quotes.Line(unitPrice: 0.125m, quantity: 1m, vatRate: 20m));

        Assert.Equal(new Money(0.13m), quote.Lines.Single().TotalExcludingVat);
    }

    [Fact]
    public void Should_ComputeTheVatOnTheTotalOfTheRate_When_SeveralLinesShareARate()
    {
        var quote = Quotes.Draft();

        quote.AddLine(Quotes.Line(unitPrice: 0.03m, quantity: 1m, vatRate: 20m));
        quote.AddLine(Quotes.Line(unitPrice: 0.03m, quantity: 1m, vatRate: 20m));

        // 20 % of 0,06 is 0,012, so 0,01; rounding each line first would give 0,02.
        Assert.Equal(new Money(0.01m), quote.TotalVat);
    }

    [Fact]
    public void Should_BreakTheVatDownByRate_When_LinesHaveDifferentRates()
    {
        var quote = Quotes.Draft();

        quote.AddLine(Quotes.Line(unitPrice: 100m, quantity: 1m, vatRate: 20m));
        quote.AddLine(Quotes.Line(unitPrice: 50m, quantity: 1m, vatRate: 10m));

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
}
