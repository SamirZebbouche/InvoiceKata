namespace InvoiceKata.Domain.Tests;

public sealed class QuoteFinalizationTests
{
    private static readonly QuoteNumber Number = new(2026, 1);
    private static readonly DateOnly IssuedOn = new(2026, 10, 18);
    private static readonly DateOnly ValidUntil = new(2026, 11, 17);
    private const string Validity = "Devis valable 30 jours, jusqu'au 17/11/2026.";

    [Fact]
    public void Should_HaveNoNumberNorDates_When_StillADraft()
    {
        var quote = Quotes.Draft();

        Assert.Equal((QuoteStatus.Draft, (QuoteNumber?)null, (DateOnly?)null, (DateOnly?)null), (quote.Status, quote.Number, quote.IssuedOn, quote.ValidUntil));
    }

    [Fact]
    public void Should_CarryItsNumberDatesAndValidityMention_When_Finalized()
    {
        var quote = DraftWithALine();

        quote.Finalize(Number, IssuedOn, ValidUntil, Validity);

        Assert.Equal((QuoteStatus.Finalized, (QuoteNumber?)Number, (DateOnly?)IssuedOn, (DateOnly?)ValidUntil), (quote.Status, quote.Number, quote.IssuedOn, quote.ValidUntil));
        Assert.Contains(new LegalMention(MentionCode.QuoteValidity, Validity), quote.Mentions);
    }

    [Fact]
    public void Should_RaiseQuoteFinalized_When_Finalized()
    {
        var quote = DraftWithALine();
        quote.TakeEvents();

        quote.Finalize(Number, IssuedOn, ValidUntil, Validity);

        Assert.Equal([new QuoteFinalized(quote.Id, Number)], quote.TakeEvents());
    }

    [Fact]
    public void Should_RefuseToFinalizeAndStayADraft_When_TheQuoteHasNoLine()
    {
        var quote = Quotes.Draft();

        Assert.Throws<QuoteWithoutLinesException>(() => quote.Finalize(Number, IssuedOn, ValidUntil, Validity));
        Assert.Equal((QuoteStatus.Draft, (QuoteNumber?)null), (quote.Status, quote.Number));
    }

    [Fact]
    public void Should_RefuseToFinalizeAgain_When_AlreadyFinalized()
    {
        var quote = Finalized();

        Assert.Throws<QuoteAlreadyFinalizedException>(() => quote.Finalize(new QuoteNumber(2026, 2), IssuedOn, ValidUntil, Validity));
        Assert.Equal(Number, quote.Number);
    }

    public static TheoryData<string, Action<Quote>> Changes => new()
    {
        { "add a line", quote => quote.AddLine(Quotes.Line(unitPrice: 50m)) },
        { "remove a line", quote => quote.RemoveLine(quote.Lines[0].Id) },
        { "put a mention", quote => quote.PutMention(new LegalMention(MentionCode.ReducedVatRate, "Taux réduit")) },
        { "remove a mention", quote => quote.RemoveMention(MentionCode.QuoteValidity) },
    };

    // A finalized quote is a legal document: nothing changes it, its mentions included (ADR-0009).
    [Theory]
    [MemberData(nameof(Changes))]
    public void Should_RefuseTheChangeAndStayAsIssued_When_Finalized(string change, Action<Quote> apply)
    {
        var quote = Finalized();
        var issued = quote.ToState();

        var refusal = Record.Exception(() => apply(quote));

        Assert.True(refusal is QuoteAlreadyFinalizedException, $"A finalized quote accepted to {change}.");
        Assert.Equivalent(issued, quote.ToState(), strict: true);
    }

    [Fact]
    public void Should_RefuseTheFinalization_When_TheValidityDoesNotEndAfterTheIssueDate()
    {
        var quote = DraftWithALine();

        Assert.Throws<ArgumentException>(() => quote.Finalize(Number, IssuedOn, IssuedOn, Validity));
        Assert.Equal(QuoteStatus.Draft, quote.Status);
    }

    [Fact]
    public void Should_RefuseTheFinalization_When_TheNumberIsOfAnotherYear()
    {
        var quote = DraftWithALine();

        Assert.Throws<ArgumentException>(() => quote.Finalize(new QuoteNumber(2025, 1), IssuedOn, ValidUntil, Validity));
        Assert.Equal(QuoteStatus.Draft, quote.Status);
    }

    private static Quote DraftWithALine()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 100m));
        return quote;
    }

    private static Quote Finalized()
    {
        var quote = DraftWithALine();
        quote.Finalize(Number, IssuedOn, ValidUntil, Validity);
        return quote;
    }
}
