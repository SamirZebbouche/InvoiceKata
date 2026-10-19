using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Quotes.Mentions;

public sealed class IssuerLegalIdentityMentionTests
{
    private static readonly FrenchMentionTexts Texts = new();

    private readonly IssuerLegalIdentityMention _rule = new(Texts);
    private readonly QuoteScope _scope = Scopes.Draft();

    [Fact]
    public void Should_PutTheIssuerLegalIdentity_When_TheQuoteIsDrafted()
    {
        var drafted = Assert.IsType<QuoteDrafted>(Assert.Single(_scope.Quote.TakeEvents()));

        _rule.Handle(drafted, _scope);

        Assert.Equal([new LegalMention(MentionCode.IssuerLegalIdentity, Texts.IssuerLegalIdentity(_scope.Issuer))], _scope.Quote.Mentions);
    }

    [Fact]
    public void Should_PutTheLegalIdentity_When_TheIssuerIsAnIndividualEntrepreneur()
    {
        var scope = Scopes.FranchiseDraft();

        _rule.Handle(Assert.Single(scope.Quote.TakeEvents()), scope);

        Assert.Equal([new LegalMention(MentionCode.IssuerLegalIdentity, Texts.IssuerLegalIdentity(scope.Issuer))], scope.Quote.Mentions);
    }

    [Fact]
    public void Should_LeaveTheMentionsUnchanged_When_TheSameEventIsHandledAgain()
    {
        var drafted = Assert.Single(_scope.Quote.TakeEvents());
        _rule.Handle(drafted, _scope);
        var first = _scope.Quote.Mentions;

        _rule.Handle(drafted, _scope);

        Assert.Equal(first, _scope.Quote.Mentions);
    }
}
