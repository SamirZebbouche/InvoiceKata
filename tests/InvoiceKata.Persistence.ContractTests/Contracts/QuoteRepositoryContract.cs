using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Persistence.ContractTests.Contracts;

public abstract class QuoteRepositoryContract
{
    protected abstract IQuoteRepository CreateRepository();

    [Fact]
    public async Task Should_GiveBackTheSameQuote_When_ItWasAddedWithLinesAndMentions()
    {
        var repository = CreateRepository();
        var quote = DraftQuote();
        quote.AddLine(Samples.Line("Faïence", 33.33m, 10m, discount: 10m));
        quote.PutMention(new LegalMention(MentionCode.ReducedVatRate, "Taux réduit"));
        quote.TakeEvents();

        await repository.AddAsync(quote, CancellationToken.None);
        var found = await repository.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);

        Assert.Equal((quote.Id, quote.IssuerId, quote.CustomerId, quote.Status), (found.Id, found.IssuerId, found.CustomerId, found.Status));
        Assert.Equal(LinesOf(quote), LinesOf(found));
        Assert.Equal(quote.Mentions, found.Mentions);
        Assert.Empty(found.TakeEvents());
    }

    [Fact]
    public async Task Should_GiveBackTheLatestState_When_TheQuoteWasSavedAgain()
    {
        var repository = CreateRepository();
        var quote = DraftQuote();
        await repository.AddAsync(quote, CancellationToken.None);

        var stored = await repository.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);
        stored.AddLine(Samples.Line("Audit", 800m, 20m));
        await repository.SaveAsync(stored, CancellationToken.None);
        var found = await repository.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);

        Assert.Equal("Audit", Assert.Single(found.Lines).Designation);
    }

    [Fact]
    public async Task Should_NotKeepAChange_When_TheQuoteWasNotSavedAgain()
    {
        var repository = CreateRepository();
        var quote = DraftQuote();
        await repository.AddAsync(quote, CancellationToken.None);

        var stored = await repository.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);
        stored.AddLine(Samples.Line("Audit", 800m, 20m));
        var found = await repository.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);

        Assert.Empty(found.Lines);
    }

    [Fact]
    public async Task Should_RefuseToGiveAQuote_When_AnotherIssuerAsksForIt()
    {
        var repository = CreateRepository();
        var quote = DraftQuote();
        await repository.AddAsync(quote, CancellationToken.None);

        await Assert.ThrowsAsync<QuoteNotFoundException>(() => repository.GetAsync(IssuerId.New(), quote.Id, CancellationToken.None));
    }

    private static Quote DraftQuote()
    {
        var issuer = Samples.Issuer();
        var quote = Quote.DraftFor(issuer, Samples.ProfessionalOf(issuer));
        quote.TakeEvents();
        return quote;
    }

    private static IEnumerable<object> LinesOf(Quote quote) => quote.Lines.Select(line =>
        (object)(line.Id, line.Designation, line.Quantity, line.Unit, line.UnitPriceExcludingVat, line.VatRate, line.Discount));
}
