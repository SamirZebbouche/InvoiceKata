using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Quotes.Mentions;

public sealed class VatFranchiseMentionTests
{
    private static readonly FrenchMentionTexts Texts = new();
    private readonly VatFranchiseMention _rule = new(Texts);

    [Fact]
    public void Should_PutTheMention_When_TheQuoteIsInFranchise()
    {
        var scope = Scopes.FranchiseDraft();

        _rule.Handle(Assert.Single(scope.Quote.TakeEvents()), scope);

        Assert.Equal([new LegalMention(MentionCode.VatFranchise, Texts.VatFranchise())], scope.Quote.Mentions);
    }

    [Fact]
    public void Should_NotPutTheMention_When_TheQuoteIsAtTheNormalRegime()
    {
        var scope = Scopes.Draft();

        _rule.Handle(Assert.Single(scope.Quote.TakeEvents()), scope);

        Assert.Empty(scope.Quote.Mentions);
    }
}
