namespace InvoiceKata.Domain.Tests;

public sealed class InvoiceIssueTests
{
    private static readonly InvoiceNumber Number = new(2026, 1);
    private static readonly DateOnly IssuedOn = new(2026, 11, 30);
    private static readonly DateOnly DueOn = new(2026, 12, 30);
    private static readonly LegalMention[] Payment =
    [
        new(MentionCode.PaymentDueDate, "Échéance : 30/12/2026."),
        new(MentionCode.EarlyPaymentDiscount, "Escompte pour paiement anticipé : néant."),
        new(MentionCode.LatePaymentPenalties, "Pénalités de retard : trois fois le taux d'intérêt légal."),
        new(MentionCode.RecoveryIndemnity, "Indemnité forfaitaire pour frais de recouvrement : 40 €."),
    ];

    [Fact]
    public void Should_CopyTheQuoteAsIssued_When_Issued()
    {
        var quote = FinalizedQuote();

        var invoice = Invoice.IssueFor(quote, Number, IssuedOn, DueOn, Payment);

        Assert.Equal((quote.IssuerId, quote.CustomerId, quote.Id, quote.Number!.Value, quote.IssuedOn!.Value), (invoice.IssuerId, invoice.CustomerId, invoice.QuoteId, invoice.QuoteNumber, invoice.QuoteIssuedOn));
        Assert.Equal(quote.Lines, invoice.Lines);
        Assert.Equal((quote.TotalExcludingVat, quote.TotalVat, quote.TotalIncludingVat), (invoice.TotalExcludingVat, invoice.TotalVat, invoice.TotalIncludingVat));
        Assert.Equal(quote.VatBreakdown, invoice.VatBreakdown);
    }

    [Fact]
    public void Should_CarryItsNumberDatesAndPaymentMentions_When_Issued()
    {
        var invoice = Invoice.IssueFor(FinalizedQuote(), Number, IssuedOn, DueOn, Payment);

        Assert.Equal((Number, IssuedOn, DueOn), (invoice.Number, invoice.IssuedOn, invoice.DueOn));
        Assert.Equal(4, invoice.Id.Value.Version);
        Assert.All(Payment, mention => Assert.Contains(mention, invoice.Mentions));
    }

    // An invoice without one of its payment mentions could not be read back: it is never issued.
    [Theory]
    [InlineData(MentionCode.PaymentDueDate)]
    [InlineData(MentionCode.EarlyPaymentDiscount)]
    [InlineData(MentionCode.LatePaymentPenalties)]
    [InlineData(MentionCode.RecoveryIndemnity)]
    public void Should_RefuseToIssue_When_APaymentMentionIsMissing(MentionCode missing)
    {
        var refusal = Record.Exception(() =>
            Invoice.IssueFor(FinalizedQuote(), Number, IssuedOn, DueOn, [.. Payment.Where(mention => mention.Code != missing)]));

        Assert.Contains("an invoice lacks a payment mention", Assert.IsType<ArgumentException>(refusal).Message, StringComparison.Ordinal);
    }

    // The quote's mentions stay on the invoice, except its validity, which only concerns the quote.
    [Fact]
    public void Should_KeepTheQuoteMentionsButItsValidity_When_Issued()
    {
        var quote = FinalizedQuote();

        var invoice = Invoice.IssueFor(quote, Number, IssuedOn, DueOn, Payment);

        Assert.Contains(new LegalMention(MentionCode.IssuerLegalIdentity, "Batifolio, SAS"), invoice.Mentions);
        Assert.DoesNotContain(invoice.Mentions, mention => mention.Code == MentionCode.QuoteValidity);
    }

    [Fact]
    public void Should_RaiseInvoiceIssued_When_Issued()
    {
        var invoice = Invoice.IssueFor(FinalizedQuote(), Number, IssuedOn, DueOn, Payment);

        Assert.Equal([new InvoiceIssued(invoice.Id, invoice.IssuerId, Number)], invoice.TakeEvents());
    }

    [Fact]
    public void Should_HandTheEventsOverOnce_When_TakenTwice()
    {
        var invoice = Invoice.IssueFor(FinalizedQuote(), Number, IssuedOn, DueOn, Payment);
        invoice.TakeEvents();

        var again = invoice.TakeEvents();

        Assert.Empty(again);
    }

    // An amount computed by a rule that changed since the quote was issued stays as issued (ADR-0009).
    [Fact]
    public void Should_CopyTheQuoteAmountsAsIssued_When_TheyDifferFromARecomputation()
    {
        var state = FinalizedQuote().ToState();
        var quote = Quote.FromState(state with
        {
            Finalization = state.Finalization! with
            {
                TotalExcludingVat = 99.99m,
                VatBreakdown = [new VatBreakdownState(20m, 99.99m, 19.99m)],
                TotalVat = 19.99m,
                TotalIncludingVat = 119.98m,
            },
        });

        var invoice = Invoice.IssueFor(quote, Number, IssuedOn, DueOn, Payment);

        Assert.Equal((new Money(99.99m), new Money(19.99m), new Money(119.98m)), (invoice.TotalExcludingVat, invoice.TotalVat, invoice.TotalIncludingVat));
    }

    [Fact]
    public void Should_RefuseToInvoice_When_TheQuoteIsADraft()
    {
        var draft = Quotes.Draft();
        draft.AddLine(Quotes.Line(unitPrice: 100m));

        Assert.Throws<QuoteNotFinalizedException>(() => Invoice.IssueFor(draft, Number, IssuedOn, DueOn, Payment));
    }

    [Fact]
    public void Should_RefuseTheIssue_When_TheDueDateIsNotAfterTheIssueDate()
    {
        Assert.Throws<ArgumentException>(() => Invoice.IssueFor(FinalizedQuote(), Number, IssuedOn, IssuedOn, Payment));
    }

    [Fact]
    public void Should_RefuseTheIssue_When_TheNumberIsOfAnotherYear()
    {
        Assert.Throws<ArgumentException>(() => Invoice.IssueFor(FinalizedQuote(), new InvoiceNumber(2025, 1), IssuedOn, DueOn, Payment));
    }

    private static Quote FinalizedQuote()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 100m));
        quote.AddLine(Quotes.Line(unitPrice: 50m, vatRate: 10m));
        quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, "Batifolio, SAS"));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return quote;
    }
}
