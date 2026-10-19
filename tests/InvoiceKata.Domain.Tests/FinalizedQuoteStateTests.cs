namespace InvoiceKata.Domain.Tests;

// Version 2 of the quote state: a finalized quote stores its number, its dates and its amounts as issued (CDR-0006).
public sealed class FinalizedQuoteStateTests
{
    private static readonly QuoteNumber Number = new(2026, 1);
    private static readonly DateOnly IssuedOn = new(2026, 10, 18);
    private static readonly DateOnly ValidUntil = new(2026, 11, 17);

    [Fact]
    public void Should_RebuildTheSameFinalizedQuoteWithoutRaisingEvents_When_RestoredFromItsState()
    {
        var quote = Finalized();

        var restored = Quote.FromState(quote.ToState());

        Assert.Equivalent(quote.ToState(), restored.ToState(), strict: true);
        Assert.Equal((QuoteStatus.Finalized, (QuoteNumber?)Number, (DateOnly?)IssuedOn, (DateOnly?)ValidUntil), (restored.Status, restored.Number, restored.IssuedOn, restored.ValidUntil));
        Assert.Empty(restored.TakeEvents());
    }

    // The issued totals add up over every rate of the breakdown, not only the first.
    [Fact]
    public void Should_RebuildTheSameFinalizedQuote_When_ItHasTwoVatRates()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 100m, vatRate: 20m));
        quote.AddLine(Quotes.Line(unitPrice: 300m, vatRate: 10m));
        quote.Finalize(Number, IssuedOn, ValidUntil, "Devis valable 30 jours, jusqu'au 17/11/2026.");

        var restored = Quote.FromState(quote.ToState());

        Assert.Equivalent(quote.ToState(), restored.ToState(), strict: true);
    }

    [Fact]
    public void Should_StillRefuseAnyChange_When_RestoredFromAFinalizedState()
    {
        var restored = Quote.FromState(Finalized().ToState());

        Assert.Throws<QuoteAlreadyFinalizedException>(() => restored.AddLine(Quotes.Line(unitPrice: 50m)));
    }

    // A rule may change after the quote was issued: the amounts stay those that were issued (ADR-0009).
    [Fact]
    public void Should_KeepTheAmountsAsIssued_When_TheyDifferFromARecomputation()
    {
        var state = Finalized().ToState();
        var issued = state.Finalization! with
        {
            TotalExcludingVat = 99.99m,
            VatBreakdown = [new VatBreakdownState(20m, 99.99m, 19.99m)],
            TotalVat = 19.99m,
            TotalIncludingVat = 119.98m,
        };

        var restored = Quote.FromState(state with { Finalization = issued });

        Assert.Equal((new Money(99.99m), new Money(19.99m), new Money(119.98m)), (restored.TotalExcludingVat, restored.TotalVat, restored.TotalIncludingVat));
        Assert.Equal([new VatBreakdownLine(new VatRate(20m), new Money(99.99m), new Money(19.99m))], restored.VatBreakdown);
    }

    [Fact]
    public void Should_KeepTheAmountsComputedAtTheFinalization_When_Finalized()
    {
        var quote = Finalized();

        var issued = quote.ToState().Finalization!;

        Assert.Equal((100m, 20m, 120m), (issued.TotalExcludingVat, issued.TotalVat, issued.TotalIncludingVat));
        Assert.Equal([new VatBreakdownState(20m, 100m, 20m)], issued.VatBreakdown);
    }

    // Upcasting on read (ADR-0009): a version 1 state is a draft, converted in memory, never rewritten.
    [Fact]
    public void Should_ReadAVersion1DraftAsTheCurrentVersion_When_RestoringAnOlderState()
    {
        var draft = Quotes.Draft();
        draft.AddLine(Quotes.Line(unitPrice: 100m));
        var version1 = draft.ToState() with { Version = 1 };

        var restored = Quote.FromState(version1);

        Assert.Equal((QuoteState.CurrentVersion, QuoteStatus.Draft), (restored.ToState().Version, restored.Status));
        Assert.Equal(draft.Lines.Count, restored.Lines.Count);
    }

    public static TheoryData<string, string, Func<object>> CorruptStates => new()
    {
        { "quote of an unknown version", "version 4 is not supported", () => Quote.FromState(Finalized().ToState() with { Version = 4 }) },
        { "finalized quote of version 1", "a version 1 quote is a draft", () => Quote.FromState(Finalized().ToState() with { Version = 1 }) },
        { "version 1 quote finalized without its finalization", "a version 1 quote is a draft", () => Quote.FromState(Finalized().ToState() with { Version = 1, Finalization = null }) },
        { "version 1 draft with a finalization", "a version 1 quote is a draft", () => Quote.FromState(Finalized().ToState() with { Version = 1, Status = QuoteStatus.Draft }) },
        { "finalized quote whose total including VAT does not add up", "the issued totals do not add up", () => Quote.FromState(WithFinalization(finalization => finalization with { TotalIncludingVat = 999m })) },
        { "finalized quote whose VAT total is not its breakdown's", "the issued totals do not add up", () => Quote.FromState(WithFinalization(finalization => finalization with { TotalVat = 1m, TotalIncludingVat = 101m })) },
        { "finalized quote whose total excluding VAT is not its breakdown's", "the issued totals do not add up", () => Quote.FromState(WithFinalization(finalization => finalization with { TotalExcludingVat = 50m, TotalIncludingVat = 70m })) },
        { "finalized quote without its finalization", "a quote is finalized if and only if it has a finalization", () => Quote.FromState(Finalized().ToState() with { Finalization = null }) },
        { "draft with a finalization", "a quote is finalized if and only if it has a finalization", () => Quote.FromState(Finalized().ToState() with { Status = QuoteStatus.Draft }) },
        { "finalized quote with an unreadable number", "number 'D-26-1' is not a quote number", () => Quote.FromState(WithFinalization(finalization => finalization with { Number = "D-26-1" })) },
        { "finalized quote numbered in another year", "number D-2025-0001 is not of the year 2026", () => Quote.FromState(WithFinalization(finalization => finalization with { Number = "D-2025-0001" })) },
        { "finalized quote valid until its issue date", "the validity does not end after the issue date", () => Quote.FromState(WithFinalization(finalization => finalization with { ValidUntil = IssuedOn })) },
        { "finalized quote without lines", "a finalized quote has no line", () => Quote.FromState(Finalized().ToState() with { Lines = [] }) },
        { "finalized quote without its validity mention", "a finalized quote has no validity mention", () => Quote.FromState(Finalized().ToState() with { Mentions = [] }) },
    };

    [Theory]
    [MemberData(nameof(CorruptStates))]
    public void Should_RefuseToRestoreAndSayWhy_When_TheFinalizedStateIsCorrupt(string corruption, string reason, Func<object> restore)
    {
        var refusal = Record.Exception(restore);

        Assert.True(refusal is ArgumentException, $"A {corruption} was restored instead of being refused.");
        Assert.Contains(reason, refusal.Message, StringComparison.Ordinal);
    }

    private static Quote Finalized()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 100m));
        quote.Finalize(Number, IssuedOn, ValidUntil, "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return quote;
    }

    private static QuoteState WithFinalization(Func<QuoteFinalizationState, QuoteFinalizationState> change)
    {
        var state = Finalized().ToState();
        return state with { Finalization = change(state.Finalization!) };
    }
}
