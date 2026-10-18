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

    private FinalizeQuote UseCase(string at) => new(_quotes, _texts, new FixedClock(DateTimeOffset.Parse(at, System.Globalization.CultureInfo.InvariantCulture)));

    private async Task<Quote> StoredDraftWithALineAsync()
    {
        var quote = _scope.Quote;
        quote.AddLine(Scopes.Line(vatRate: 20m));
        await _quotes.AddAsync(quote, CancellationToken.None);
        return quote;
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    // The port, faked: it stores states, as the real adapters do, so an unsaved change is not kept.
    private sealed class StoredQuotes : IQuoteRepository
    {
        private readonly Dictionary<Guid, QuoteState> _states = [];

        public Task AddAsync(Quote quote, CancellationToken cancellationToken) => SaveAsync(quote, cancellationToken);

        public Task SaveAsync(Quote quote, CancellationToken cancellationToken)
        {
            _states[quote.Id.Value] = quote.ToState();
            return Task.CompletedTask;
        }

        public Task<Quote> GetAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken) =>
            Task.FromResult(Quote.FromState(_states[quoteId.Value]));

        public Task<IReadOnlyList<QuoteNumber>> TakenNumbersAsync(IssuerId issuerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuoteNumber>>(
                [.. _states.Values.Where(state => state.IssuerId == issuerId.Value && state.Finalization is not null)
                    .Select(state => QuoteNumber.Parse(state.Finalization!.Number))]);
    }
}
