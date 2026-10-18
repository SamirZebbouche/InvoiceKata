namespace InvoiceKata.Domain.Tests;

public sealed class QuoteLineRemovalTests
{
    [Fact]
    public void Should_RemoveTheLineFromTheTotals_When_ItIsRemoved()
    {
        var quote = Quotes.Draft();
        var kept = quote.AddLine(Quotes.Line(unitPrice: 100m));
        var removed = quote.AddLine(Quotes.Line(unitPrice: 50m));

        quote.RemoveLine(removed);

        Assert.Equal(kept, Assert.Single(quote.Lines).Id);
        Assert.Equal(new Money(100m), quote.TotalExcludingVat);
    }

    [Fact]
    public void Should_RefuseToRemoveALine_When_ItIsNotOnTheQuote()
    {
        var quote = Quotes.Draft();

        Assert.Throws<QuoteLineNotFoundException>(() => quote.RemoveLine(QuoteLineId.New()));
    }
}
