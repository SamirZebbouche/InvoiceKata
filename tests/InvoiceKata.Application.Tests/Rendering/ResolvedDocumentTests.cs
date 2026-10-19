using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Rendering;

public sealed class ResolvedDocumentTests
{
    private readonly QuoteScope _scope = Scopes.Draft();

    [Fact]
    public void Should_CarryTheKindNumberAndDates_When_ResolvingAFinalizedQuote()
    {
        var document = ResolvedDocument.OfQuote(Finalized());

        Assert.Equal(new DocumentHeader("quote", "D-2026-0001", "2026-10-18", "2026-11-17", AcceptanceBox: true), document.Document);
    }

    [Fact]
    public void Should_CarryTheIssuerAndTheCustomer_When_ResolvingAQuote()
    {
        var document = ResolvedDocument.OfQuote(Finalized());

        Assert.Equivalent(new DocumentIssuer("Batifolio", "SAS", "912345678", "FR12912345678", ["12 rue des Artisans", "47000 Agen"]), document.Issuer, strict: true);
        Assert.Equivalent(new DocumentCustomer("Les Toits Rouges SARL", ["4 place du Marché", "33000 Bordeaux"], "834567890", "FR45834567890"), document.Customer, strict: true);
    }

    // Amounts travel as invariant strings, read as decimals by the template (ADR-0005): never as floats.
    [Fact]
    public void Should_GiveEachLineWithItsAmountsAsInvariantText_When_ResolvingAQuote()
    {
        _scope.Quote.AddLine(new NewQuoteLine
        {
            Designation = "Faïence",
            Quantity = new Quantity(18.72m),
            Unit = "m²",
            UnitPriceExcludingVat = new Money(33.33m),
            VatRate = new VatRate(10m),
            Discount = new DiscountRate(10m),
        });

        var document = ResolvedDocument.OfQuote(Finalized());

        Assert.Equal(
            [
                new DocumentLine("Faïence", "18.72", "m²", "33.33", "10", "29.997", "561.54", "10"),
                new DocumentLine("Prestation", "1", "u", "100", null, "100", "100.00", "20"),
            ],
            document.Lines);
    }

    [Fact]
    public void Should_GiveTheBreakdownTotalsAndMentionsAsIssued_When_ResolvingAFinalizedQuote()
    {
        var document = ResolvedDocument.OfQuote(Finalized());

        Assert.Equal([new DocumentVatRate("20", "100.00", "20.00")], document.VatBreakdown);
        Assert.Equal(new DocumentTotals("100.00", "20.00", "120.00"), document.Totals);
        Assert.Equal([new DocumentMention("QuoteValidity", "Devis valable 30 jours, jusqu'au 17/11/2026.")], document.Mentions);
    }

    // A draft has no number nor dates yet: it resolves with empty ones, which the final validator refuses.
    [Fact]
    public void Should_LeaveTheNumberAndDatesEmpty_When_ResolvingADraft()
    {
        var document = ResolvedDocument.OfQuote(_scope);

        Assert.Equal(new DocumentHeader("quote", "", "", null, AcceptanceBox: true), document.Document);
    }

    [Fact]
    public void Should_CarryTheKindNumberDatesAndQuoteReference_When_ResolvingAnInvoice()
    {
        var document = ResolvedDocument.OfInvoice(Invoiced(), _scope.Issuer, _scope.Customer);

        Assert.Equal(
            new DocumentHeader("invoice", "F-2026-0001", "2026-11-30", null, AcceptanceBox: false, new QuoteReference("D-2026-0001", "2026-10-18")),
            document.Document);
    }

    [Fact]
    public void Should_GiveTheInvoiceLinesTotalsAndMentionsAsIssued_When_ResolvingAnInvoice()
    {
        var document = ResolvedDocument.OfInvoice(Invoiced(), _scope.Issuer, _scope.Customer);

        Assert.Equal([new DocumentLine("Prestation", "1", "u", "100", null, "100", "100.00", "20")], document.Lines);
        Assert.Equal(new DocumentTotals("100.00", "20.00", "120.00"), document.Totals);
        Assert.Equal(["PaymentDueDate", "EarlyPaymentDiscount", "LatePaymentPenalties", "RecoveryIndemnity"], document.Mentions.Select(mention => mention.Code));
        Assert.Equal("Les Toits Rouges SARL", document.Customer.Name);
    }

    // An individual has no SIREN nor VAT number: the document leaves them out rather than writing them empty.
    [Fact]
    public void Should_LeaveTheSirenAndVatNumberOut_When_TheCustomerIsAnIndividual()
    {
        var individual = Customer.RegisterIndividual(_scope.Issuer.Id, "Jeanne Martin", new Address { Street = "3 rue des Lilas", PostalCode = "33000", City = "Bordeaux" });

        var document = ResolvedDocument.OfQuote(_scope with { Customer = individual });

        Assert.Equivalent(new DocumentCustomer("Jeanne Martin", ["3 rue des Lilas", "33000 Bordeaux"], null, null), document.Customer, strict: true);
    }

    private Invoice Invoiced() => Invoice.IssueFor(Finalized().Quote, new InvoiceNumber(2026, 1), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30),
        new PaymentMentionTexts("Échéance : 30/12/2026.", "Escompte : néant.", "Pénalités : trois fois le taux légal.", "Indemnité : 40 €."));

    private QuoteScope Finalized()
    {
        _scope.Quote.AddLine(Scopes.Line(vatRate: 20m));
        _scope.Quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return _scope;
    }
}
