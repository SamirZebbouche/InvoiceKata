namespace InvoiceKata.Domain.Tests;

public sealed class QuoteMentionsTests
{
    [Fact]
    public void Should_KeepASingleMentionPerCode_When_TheSameMentionIsPutTwice()
    {
        var quote = Quotes.Draft();

        quote.PutMention(new LegalMention(MentionCode.ReducedVatRate, "first"));
        quote.PutMention(new LegalMention(MentionCode.ReducedVatRate, "second"));

        Assert.Equal([new LegalMention(MentionCode.ReducedVatRate, "second")], quote.Mentions);
    }

    [Fact]
    public void Should_DropTheMention_When_ItIsRemoved()
    {
        var quote = Quotes.Draft();
        quote.PutMention(new LegalMention(MentionCode.ReducedVatRate, "text"));

        quote.RemoveMention(MentionCode.ReducedVatRate);

        Assert.Empty(quote.Mentions);
    }

    [Fact]
    public void Should_DoNothing_When_RemovingAMentionThatIsNotThere()
    {
        var quote = Quotes.Draft();

        quote.RemoveMention(MentionCode.ReducedVatRate);

        Assert.Empty(quote.Mentions);
    }
}
