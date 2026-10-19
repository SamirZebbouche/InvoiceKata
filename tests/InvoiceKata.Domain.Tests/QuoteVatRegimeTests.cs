namespace InvoiceKata.Domain.Tests;

// The VAT regime of a quote is its issuer's (ADR-0011): a Strategy, normal or franchise.
public sealed class QuoteVatRegimeTests
{
    [Fact]
    public void Should_KeepTheVatRegimeOfItsIssuer_When_Drafted()
    {
        Assert.Equal((VatRegime.Normal, VatRegime.Franchise), (Quotes.Draft().VatRegime, FranchiseDraft().VatRegime));
    }

    // An individual entrepreneur bills no VAT: no breakdown, and the total including VAT is the total.
    [Fact]
    public void Should_ComputeNoVat_When_TheQuoteIsInFranchise()
    {
        var quote = FranchiseDraft();

        quote.AddLine(Quotes.Line(unitPrice: 800m, quantity: 2m, vatRate: 0m));

        Assert.Equal((new Money(1600m), Money.Zero, new Money(1600m)), (quote.TotalExcludingVat, quote.TotalVat, quote.TotalIncludingVat));
        Assert.Empty(quote.VatBreakdown);
    }

    // Better refuse than issue a wrong invoice (ADR-0002): a franchise quote takes no VAT rate.
    [Fact]
    public void Should_RefuseALineWithAVatRate_When_TheQuoteIsInFranchise()
    {
        var quote = FranchiseDraft();

        var refusal = Assert.Throws<VatRateNotApplicableException>(() => quote.AddLine(Quotes.Line(unitPrice: 800m, vatRate: 20m)));

        Assert.Equal((quote.Id, new VatRate(20m)), (refusal.QuoteId, refusal.VatRate));
        Assert.Empty(quote.Lines);
    }

    [Fact]
    public void Should_StillComputeTheVatPerRate_When_TheQuoteIsAtTheNormalRegime()
    {
        var quote = Quotes.Draft();

        quote.AddLine(Quotes.Line(unitPrice: 800m, quantity: 2m, vatRate: 20m));

        Assert.Equal([new VatBreakdownLine(new VatRate(20m), new Money(1600m), new Money(320m))], quote.VatBreakdown);
    }

    [Fact]
    public void Should_RebuildTheSameFranchiseQuote_When_RestoredFromItsState()
    {
        var quote = FranchiseDraft();
        quote.AddLine(Quotes.Line(unitPrice: 800m, vatRate: 0m));

        var restored = Quote.FromState(quote.ToState());

        Assert.Equivalent(quote.ToState(), restored.ToState(), strict: true);
        Assert.Throws<VatRateNotApplicableException>(() => restored.AddLine(Quotes.Line(unitPrice: 1m, vatRate: 10m)));
    }

    // In franchise the breakdown is empty: the issued totals still add up, with no VAT at all.
    [Fact]
    public void Should_RebuildTheFinalizedFranchiseQuote_When_RestoredFromItsState()
    {
        var quote = FranchiseDraft();
        quote.AddLine(Quotes.Line(unitPrice: 800m, quantity: 2m, vatRate: 0m));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");

        var restored = Quote.FromState(quote.ToState());

        Assert.Equal((new Money(1600m), Money.Zero, new Money(1600m)), (restored.TotalExcludingVat, restored.TotalVat, restored.TotalIncludingVat));
    }

    [Fact]
    public void Should_RebuildTheInvoiceOfAFranchiseQuote_When_RestoredFromItsState()
    {
        var quote = FranchiseDraft();
        quote.AddLine(Quotes.Line(unitPrice: 800m, quantity: 2m, vatRate: 0m));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        var invoice = Invoice.IssueFor(quote, new InvoiceNumber(2026, 1), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30),
            new PaymentMentionTexts("Échéance : 30/12/2026.", "Escompte : néant.", "Pénalités : trois fois le taux légal.", "Indemnité : 40 €."));

        var restored = Invoice.FromState(invoice.ToState());

        Assert.Equal((new Money(1600m), Money.Zero, new Money(1600m)), (restored.TotalExcludingVat, restored.TotalVat, restored.TotalIncludingVat));
    }

    // Upcasting on read (ADR-0009): a version 2 quote is at the normal regime.
    [Fact]
    public void Should_ReadAVersion2QuoteAtTheNormalRegime_When_RestoringAnOlderState()
    {
        var version2 = Quotes.Draft().ToState() with { Version = 2 };

        var restored = Quote.FromState(version2);

        Assert.Equal((QuoteState.CurrentVersion, VatRegime.Normal), (restored.ToState().Version, restored.VatRegime));
    }

    public static TheoryData<string, string, Func<object>> CorruptStates => new()
    {
        { "quote of an unknown VAT regime", "VAT regime 99 is unknown", () => Quote.FromState(Quotes.Draft().ToState() with { VatRegime = (VatRegime)99 }) },
        {
            "franchise quote with a VAT rate", "a quote in VAT franchise has a line with a VAT rate",
            () => Quote.FromState(FranchiseState() with { Lines = [FranchiseState().Lines[0] with { VatRate = 20m }] })
        },
        { "version 2 quote that is not at the normal regime", "a version 2 quote is at the normal VAT regime", () => Quote.FromState(FranchiseState() with { Version = 2 }) },
        {
            "VAT without breakdown", "the issued totals do not add up",
            () => Quote.FromState(FinalizedFranchiseState() with { Finalization = FinalizedFranchiseState().Finalization! with { TotalVat = 1m, TotalIncludingVat = 1601m } })
        },
    };

    [Theory]
    [MemberData(nameof(CorruptStates))]
    public void Should_RefuseToRestoreAndSayWhy_When_TheStateIsCorrupt(string corruption, string reason, Func<object> restore)
    {
        var refusal = Record.Exception(restore);

        Assert.True(refusal is ArgumentException, $"A {corruption} was restored instead of being refused.");
        Assert.Contains(reason, refusal.Message, StringComparison.Ordinal);
    }

    private static Quote FranchiseDraft()
    {
        var issuer = Issuer.ConfigureIndividualEntrepreneur(
            "Jean Dupont", new Siren("853123456"), new Address { Street = "8 chemin des Vignes", PostalCode = "47000", City = "Agen" });
        return Quote.DraftFor(issuer, Quotes.ProfessionalCustomerOf(issuer));
    }

    private static QuoteState FinalizedFranchiseState()
    {
        var quote = FranchiseDraft();
        quote.AddLine(Quotes.Line(unitPrice: 800m, quantity: 2m, vatRate: 0m));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return quote.ToState();
    }

    private static QuoteState FranchiseState()
    {
        var quote = FranchiseDraft();
        quote.AddLine(Quotes.Line(unitPrice: 800m, vatRate: 0m));
        return quote.ToState();
    }
}
