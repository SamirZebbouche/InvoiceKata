using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Quotes.Mentions;

public sealed class ReducedVatRateMentionTests
{
    private static readonly FrenchMentionTexts Texts = new();
    private static readonly LegalMention Expected = new(MentionCode.ReducedVatRate, Texts.ReducedVatRate());

    private readonly ReducedVatRateMention _rule = new(Texts);
    private readonly QuoteScope _scope = Scopes.Draft();

    [Fact]
    public void Should_PutTheMention_When_ALineIsAtTheReducedRate()
    {
        _scope.Quote.AddLine(Scopes.Line(vatRate: 10m));

        HandlePendingEvents();

        Assert.Equal([Expected], _scope.Quote.Mentions);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(5.5)]
    [InlineData(0)]
    public void Should_NotPutTheMention_When_NoLineIsAtTheReducedRate(decimal vatRate)
    {
        _scope.Quote.AddLine(Scopes.Line(vatRate));

        HandlePendingEvents();

        Assert.Empty(_scope.Quote.Mentions);
    }

    [Fact]
    public void Should_KeepTheMention_When_AnotherLineAtTheReducedRateRemains()
    {
        var removed = _scope.Quote.AddLine(Scopes.Line(vatRate: 10m));
        _scope.Quote.AddLine(Scopes.Line(vatRate: 10m));
        HandlePendingEvents();

        _scope.Quote.RemoveLine(removed);
        HandlePendingEvents();

        Assert.Equal([Expected], _scope.Quote.Mentions);
    }

    [Fact]
    public void Should_RemoveTheMention_When_TheLastLineAtTheReducedRateIsRemoved()
    {
        var reduced = _scope.Quote.AddLine(Scopes.Line(vatRate: 10m));
        _scope.Quote.AddLine(Scopes.Line(vatRate: 20m));
        HandlePendingEvents();

        _scope.Quote.RemoveLine(reduced);
        HandlePendingEvents();

        Assert.Empty(_scope.Quote.Mentions);
    }

    [Fact]
    public void Should_LeaveTheMentionsUnchanged_When_TheSameEventIsHandledAgain()
    {
        _scope.Quote.AddLine(Scopes.Line(vatRate: 10m));
        var added = Assert.IsType<QuoteLineAdded>(_scope.Quote.TakeEvents()[^1]);
        _rule.Handle(added, _scope);

        _rule.Handle(added, _scope);

        Assert.Equal([Expected], _scope.Quote.Mentions);
    }

    private void HandlePendingEvents()
    {
        foreach (var quoteEvent in _scope.Quote.TakeEvents())
        {
            _rule.Handle(quoteEvent, _scope);
        }
    }
}
