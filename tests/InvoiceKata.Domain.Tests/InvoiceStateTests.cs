namespace InvoiceKata.Domain.Tests;

public sealed class InvoiceStateTests
{
    [Fact]
    public void Should_RebuildTheSameInvoiceWithoutRaisingEvents_When_RestoredFromItsState()
    {
        var invoice = Issued();

        var restored = Invoice.FromState(invoice.ToState());

        Assert.Equivalent(invoice.ToState(), restored.ToState(), strict: true);
        Assert.Equal((invoice.Number, invoice.TotalIncludingVat), (restored.Number, restored.TotalIncludingVat));
        Assert.Empty(restored.TakeEvents());
    }

    // A rule may change after the invoice was issued: its amounts stay those that were issued (ADR-0009).
    [Fact]
    public void Should_KeepTheAmountsAsIssued_When_TheyDifferFromARecomputation()
    {
        var state = Issued().ToState() with
        {
            TotalExcludingVat = 99.99m,
            VatBreakdown = [new VatBreakdownState(20m, 99.99m, 19.99m)],
            TotalVat = 19.99m,
            TotalIncludingVat = 119.98m,
        };

        var restored = Invoice.FromState(state);

        Assert.Equal((new Money(99.99m), new Money(19.99m), new Money(119.98m)), (restored.TotalExcludingVat, restored.TotalVat, restored.TotalIncludingVat));
    }

    public static TheoryData<string, string, Func<object>> CorruptStates => new()
    {
        { "unknown version", "version 2 is not supported", () => Invoice.FromState(State() with { Version = 2 }) },
        { "no identifier", "the invoice, its issuer, its customer or its quote has no identifier", () => Invoice.FromState(State() with { Id = Guid.Empty }) },
        { "no quote", "the invoice, its issuer, its customer or its quote has no identifier", () => Invoice.FromState(State() with { QuoteId = Guid.Empty }) },
        { "unreadable number", "number 'F-26-1' is not an invoice number", () => Invoice.FromState(State() with { Number = "F-26-1" }) },
        { "number of another year", "number F-2025-0001 is not of the year 2026", () => Invoice.FromState(State() with { Number = "F-2025-0001" }) },
        { "unreadable quote number", "quote number 'D-1' is not a quote number", () => Invoice.FromState(State() with { QuoteNumber = "D-1" }) },
        { "due before issue", "the due date does not come after the issue date", () => Invoice.FromState(State() with { DueOn = new DateOnly(2026, 11, 30) }) },
        { "no line", "an invoice has no line", () => Invoice.FromState(State() with { Lines = [] }) },
        { "two lines with one id", "two lines share an identifier", () => Invoice.FromState(WithLines(state => [state.Lines[0], state.Lines[0]])) },
        { "totals not adding up", "the issued totals do not add up", () => Invoice.FromState(State() with { TotalIncludingVat = 999m }) },
        { "VAT total not its breakdown's", "the issued totals do not add up", () => Invoice.FromState(State() with { TotalVat = 1m, TotalIncludingVat = 101m }) },
        { "total excluding VAT not its breakdown's", "the issued totals do not add up", () => Invoice.FromState(State() with { TotalExcludingVat = 50m, TotalIncludingVat = 70m }) },
        { "unknown mention code", "a mention code is unknown", () => Invoice.FromState(State() with { Mentions = [.. State().Mentions, new LegalMention((MentionCode)99, "?")] }) },
        { "two mentions with one code", "two mentions share a code", () => Invoice.FromState(WithMentions(state => [.. state.Mentions, state.Mentions[0]])) },
        {
            "no payment mention", "an invoice lacks a payment mention",
            () => Invoice.FromState(State() with { Mentions = [.. State().Mentions.Where(mention => mention.Code != MentionCode.LatePaymentPenalties)] })
        },
    };

    // The indemnity depends on the customer, whom the invoice does not know: the final validator checks it (ADR-0012).
    [Fact]
    public void Should_RestoreTheInvoice_When_ItHasNoRecoveryIndemnity()
    {
        var state = State() with { Mentions = [.. State().Mentions.Where(mention => mention.Code != MentionCode.RecoveryIndemnity)] };

        var restored = Invoice.FromState(state);

        Assert.DoesNotContain(restored.Mentions, mention => mention.Code == MentionCode.RecoveryIndemnity);
    }

    [Theory]
    [MemberData(nameof(CorruptStates))]
    public void Should_RefuseToRestoreAndSayWhy_When_TheStateIsCorrupt(string corruption, string reason, Func<object> restore)
    {
        var refusal = Record.Exception(restore);

        Assert.True(refusal is ArgumentException, $"An invoice with {corruption} was restored instead of being refused.");
        Assert.Contains(reason, refusal.Message, StringComparison.Ordinal);
    }

    private static InvoiceState State() => Issued().ToState();

    private static InvoiceState WithLines(Func<InvoiceState, IReadOnlyList<QuoteLineState>> lines)
    {
        var state = State();
        return state with { Lines = lines(state) };
    }

    private static InvoiceState WithMentions(Func<InvoiceState, IReadOnlyList<LegalMention>> mentions)
    {
        var state = State();
        return state with { Mentions = mentions(state) };
    }

    private static Invoice Issued()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 100m));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return Invoice.IssueFor(quote, new InvoiceNumber(2026, 1), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30), [
            new(MentionCode.PaymentDueDate, "Échéance : 30/12/2026."),
            new(MentionCode.EarlyPaymentDiscount, "Escompte pour paiement anticipé : néant."),
            new(MentionCode.LatePaymentPenalties, "Pénalités de retard : trois fois le taux d'intérêt légal."),
            new(MentionCode.RecoveryIndemnity, "Indemnité forfaitaire pour frais de recouvrement : 40 €."),
        ]);
    }
}
