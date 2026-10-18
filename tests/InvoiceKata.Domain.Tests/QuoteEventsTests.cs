namespace InvoiceKata.Domain.Tests;

public sealed class QuoteEventsTests
{
    [Fact]
    public void Should_RaiseQuoteDrafted_When_TheQuoteIsDrafted()
    {
        var quote = Quotes.Draft();

        Assert.Equal([new QuoteDrafted(quote.Id)], quote.TakeEvents());
    }

    [Fact]
    public void Should_RaiseAnEventPerLineChange_When_LinesAreAddedAndRemoved()
    {
        var quote = Quotes.Draft();
        quote.TakeEvents();

        var line = quote.AddLine(Quotes.Line(unitPrice: 100m));
        quote.RemoveLine(line);

        Assert.Equal([new QuoteLineAdded(quote.Id, line), new QuoteLineRemoved(quote.Id, line)], quote.TakeEvents());
    }

    [Fact]
    public void Should_HaveNoEventLeft_When_EventsHaveBeenTaken()
    {
        var quote = Quotes.Draft();

        quote.TakeEvents();

        Assert.Empty(quote.TakeEvents());
    }
}
