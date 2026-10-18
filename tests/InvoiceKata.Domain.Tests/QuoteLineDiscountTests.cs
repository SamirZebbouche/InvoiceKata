namespace InvoiceKata.Domain.Tests;

public sealed class QuoteLineDiscountTests
{
    [Fact]
    public void Should_KeepTheNetUnitPriceUnrounded_When_ADiscountApplies()
    {
        var quote = Quotes.Draft();

        quote.AddLine(Quotes.Line(unitPrice: 33.33m, quantity: 18.72m, vatRate: 10m) with { Discount = new DiscountRate(10m) });

        var line = quote.Lines.Single();
        Assert.Equal(new Money(29.997m), line.NetUnitPriceExcludingVat);
        // 18,72 × 29,997 = 561,543…; a net price rounded to 30,00 would give 561,60.
        Assert.Equal(new Money(561.54m), line.TotalExcludingVat);
    }

    [Fact]
    public void Should_UseTheUnitPriceAsNetPrice_When_NoDiscountApplies()
    {
        var quote = Quotes.Draft();

        quote.AddLine(Quotes.Line(unitPrice: 33.33m));

        Assert.Equal(new Money(33.33m), quote.Lines.Single().NetUnitPriceExcludingVat);
    }
}
