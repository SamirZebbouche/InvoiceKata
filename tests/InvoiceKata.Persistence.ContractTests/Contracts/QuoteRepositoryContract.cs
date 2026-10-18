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
    public async Task Should_GiveBackAFinalizedQuoteAsIssued_When_ItWasSaved()
    {
        var repository = CreateRepository();
        var quote = DraftQuote();
        quote.AddLine(Samples.Line("Faïence", 33.33m, 10m, discount: 10m));
        await repository.AddAsync(quote, CancellationToken.None);

        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        await repository.SaveAsync(quote, CancellationToken.None);
        var found = await repository.GetAsync(quote.IssuerId, quote.Id, CancellationToken.None);

        Assert.Equivalent(quote.ToState(), found.ToState(), strict: true);
    }

    // Only this issuer's finalized quotes have a number: drafts have none, and another issuer counts on its own.
    [Fact]
    public async Task Should_GiveTheNumbersOfThisIssuersFinalizedQuotesOnly_When_AskedForTheTakenNumbers()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        await FinalizedAsync(repository, issuer, new QuoteNumber(2026, 1));
        await FinalizedAsync(repository, issuer, new QuoteNumber(2026, 2));
        await repository.AddAsync(DraftQuote(issuer), CancellationToken.None);
        await FinalizedAsync(repository, Samples.Issuer(), new QuoteNumber(2026, 3));

        var taken = await repository.TakenNumbersAsync(issuer.Id, CancellationToken.None);

        Assert.Equal([new QuoteNumber(2026, 1), new QuoteNumber(2026, 2)], taken.OrderBy(number => number.Sequence));
    }

    // The port's half of the guarantee (CDR-0007): a concurrent finalization could have computed the same number.
    [Fact]
    public async Task Should_RefuseToSaveAFinalizedQuoteAndKeepItsDraft_When_AnotherQuoteOfTheIssuerHasItsNumber()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        await FinalizedAsync(repository, issuer, new QuoteNumber(2026, 1));
        var quote = DraftQuote(issuer);
        quote.AddLine(Samples.Line("Faïence", 33.33m, 10m));
        await repository.AddAsync(quote, CancellationToken.None);

        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        await Assert.ThrowsAsync<QuoteNumberAlreadyTakenException>(() => repository.SaveAsync(quote, CancellationToken.None));

        Assert.Equal(QuoteStatus.Draft, (await repository.GetAsync(issuer.Id, quote.Id, CancellationToken.None)).Status);
    }

    [Fact]
    public async Task Should_AcceptTheSameNumber_When_TheQuotesBelongToDifferentIssuers()
    {
        var repository = CreateRepository();
        await FinalizedAsync(repository, Samples.Issuer(), new QuoteNumber(2026, 1));

        await FinalizedAsync(repository, Samples.Issuer(), new QuoteNumber(2026, 1));
    }

    // A finalized quote is a frozen document: it is never written over (ADR-0006), even with itself.
    [Fact]
    public async Task Should_RefuseToWriteOverAFinalizedQuote_When_ItIsSavedAgain()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        var finalized = await FinalizedAsync(repository, issuer, new QuoteNumber(2026, 1));

        await Assert.ThrowsAsync<QuoteAlreadyFinalizedException>(() => repository.SaveAsync(finalized, CancellationToken.None));
    }

    [Fact]
    public async Task Should_GiveNoNumber_When_TheIssuerHasNoQuote()
    {
        var taken = await CreateRepository().TakenNumbersAsync(IssuerId.New(), CancellationToken.None);

        Assert.Empty(taken);
    }

    [Fact]
    public async Task Should_RefuseToGiveAQuote_When_AnotherIssuerAsksForIt()
    {
        var repository = CreateRepository();
        var quote = DraftQuote();
        await repository.AddAsync(quote, CancellationToken.None);

        await Assert.ThrowsAsync<QuoteNotFoundException>(() => repository.GetAsync(IssuerId.New(), quote.Id, CancellationToken.None));
    }

    private static Quote DraftQuote() => DraftQuote(Samples.Issuer());

    private static Quote DraftQuote(Issuer issuer)
    {
        var quote = Quote.DraftFor(issuer, Samples.ProfessionalOf(issuer));
        quote.TakeEvents();
        return quote;
    }

    private static async Task<Quote> FinalizedAsync(IQuoteRepository repository, Issuer issuer, QuoteNumber number)
    {
        var quote = DraftQuote(issuer);
        quote.AddLine(Samples.Line("Faïence", 33.33m, 10m));
        quote.Finalize(number, new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        await repository.AddAsync(quote, CancellationToken.None);
        return quote;
    }

    private static IEnumerable<object> LinesOf(Quote quote) => quote.Lines.Select(line =>
        (object)(line.Id, line.Designation, line.Quantity, line.Unit, line.UnitPriceExcludingVat, line.VatRate, line.Discount));
}
