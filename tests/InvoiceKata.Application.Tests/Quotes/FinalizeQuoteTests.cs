using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Quotes;

public sealed class FinalizeQuoteTests
{
    private readonly StoredQuotes _quotes = new();
    private readonly FrenchMentionTexts _texts = new();
    private readonly QuoteScope _scope = Scopes.Draft();

    [Fact]
    public async Task Should_IssueTheQuoteTodayWithTheFirstNumberOfTheYear_When_TheIssuerHasNone()
    {
        var quote = await StoredDraftWithALineAsync();

        await UseCase(at: "2026-10-18T10:00:00Z").HandleAsync(quote.IssuerId, quote.Id, FinalizeQuote.DefaultValidityDays, CancellationToken.None);

        var finalized = await _quotes.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);
        Assert.Equal(
            (QuoteStatus.Finalized, (QuoteNumber?)new QuoteNumber(2026, 1), (DateOnly?)new DateOnly(2026, 10, 18), (DateOnly?)new DateOnly(2026, 11, 17)),
            (finalized.Status, finalized.Number, finalized.IssuedOn, finalized.ValidUntil));
        Assert.Contains(new LegalMention(MentionCode.QuoteValidity, _texts.QuoteValidity(30, new DateOnly(2026, 11, 17))), finalized.Mentions);
    }

    [Fact]
    public async Task Should_FollowTheGreatestNumberTaken_When_TheIssuerFinalizedQuotesBefore()
    {
        var earlier = Quote.DraftFor(_scope.Issuer, _scope.Customer);
        earlier.AddLine(Scopes.Line(vatRate: 20m));
        earlier.Finalize(new QuoteNumber(2026, 7), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), "Valable");
        await _quotes.AddAsync(earlier, CancellationToken.None);
        var quote = await StoredDraftWithALineAsync();

        await UseCase(at: "2026-10-18T10:00:00Z").HandleAsync(quote.IssuerId, quote.Id, FinalizeQuote.DefaultValidityDays, CancellationToken.None);

        Assert.Equal(new QuoteNumber(2026, 8), (await _quotes.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None)).Number);
    }

    // The quote is dated in France: at 23:30 UTC on 31 December, it is already 1 January in Paris.
    [Fact]
    public async Task Should_DateTheQuoteInParis_When_ItIsAlreadyTheNextDayThere()
    {
        var quote = await StoredDraftWithALineAsync();

        await UseCase(at: "2026-12-31T23:30:00Z").HandleAsync(quote.IssuerId, quote.Id, FinalizeQuote.DefaultValidityDays, CancellationToken.None);

        var finalized = await _quotes.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);
        Assert.Equal(((DateOnly?)new DateOnly(2027, 1, 1), (QuoteNumber?)new QuoteNumber(2027, 1)), (finalized.IssuedOn, finalized.Number));
    }

    [Fact]
    public async Task Should_EndTheValidityAfterTheChosenNumberOfDays_When_ADurationIsGiven()
    {
        var quote = await StoredDraftWithALineAsync();

        await UseCase(at: "2026-10-18T10:00:00Z").HandleAsync(quote.IssuerId, quote.Id, 15, CancellationToken.None);

        var finalized = await _quotes.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);
        Assert.Equal((DateOnly?)new DateOnly(2026, 11, 2), finalized.ValidUntil);
        Assert.Contains(new LegalMention(MentionCode.QuoteValidity, _texts.QuoteValidity(15, new DateOnly(2026, 11, 2))), finalized.Mentions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Should_RefuseTheValidityAndKeepTheDraft_When_ItIsNotAtLeastOneDay(int validityDays)
    {
        var quote = await StoredDraftWithALineAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            UseCase(at: "2026-10-18T10:00:00Z").HandleAsync(quote.IssuerId, quote.Id, validityDays, CancellationToken.None));

        Assert.Equal(QuoteStatus.Draft, (await _quotes.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None)).Status);
    }

    // The quote finalized is already saved when its reactions hear of it (ADR-0003): none starts from an unrecorded fact.
    [Fact]
    public async Task Should_PublishQuoteFinalizedAfterTheSave_When_TheQuoteIsFinalized()
    {
        var quote = await StoredDraftWithALineAsync();
        var heard = new List<(IQuoteEvent Event, QuoteStatus Stored)>();
        var reaction = new ReadingReaction(async quoteEvent => heard.Add((quoteEvent, (await _quotes.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None)).Status)));

        await UseCase(at: "2026-10-18T10:00:00Z", reaction).HandleAsync(quote.IssuerId, quote.Id, FinalizeQuote.DefaultValidityDays, CancellationToken.None);

        Assert.Equal([(new QuoteFinalized(quote.Id, quote.IssuerId, new QuoteNumber(2026, 1)), QuoteStatus.Finalized)], heard.Select(entry => ((IQuoteEvent)entry.Event, entry.Stored)));
    }

    private FinalizeQuote UseCase(string at, params IQuoteReaction[] reactions) => new(
        _quotes, _texts, new FixedClock(DateTimeOffset.Parse(at, System.Globalization.CultureInfo.InvariantCulture)),
        new QuoteReactions(reactions, Microsoft.Extensions.Logging.Abstractions.NullLogger<QuoteReactions>.Instance));

    private sealed class ReadingReaction(Func<IQuoteEvent, Task> react) : IQuoteReaction
    {
        public Task ReactAsync(IQuoteEvent quoteEvent, CancellationToken cancellationToken) => react(quoteEvent);
    }

    private async Task<Quote> StoredDraftWithALineAsync()
    {
        var quote = _scope.Quote;
        quote.AddLine(Scopes.Line(vatRate: 20m));
        await _quotes.AddAsync(quote, CancellationToken.None);
        return quote;
    }
}
