using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Rendering;

public sealed class FinalDocumentValidatorTests
{
    private static readonly FinalDocumentValidator Validator = new(
    [
        new HeaderValidator(), new IssuerValidator(), new CustomerValidator(), new LinesValidator(), new TotalsValidator(), new MentionsValidator(),
    ]);

    [Fact]
    public void Should_AcceptTheDocument_When_EveryPartIsComplete()
    {
        var refusal = Record.Exception(() => Validator.Validate(ValidDocument()));

        Assert.Null(refusal);
    }

    public static TheoryData<string, string, Func<ResolvedDocument, ResolvedDocument>> Defects => new()
    {
        { "unknown kind", "the document kind 'letter' is unknown", document => document with { Document = document.Document with { Kind = "letter" } } },
        { "draft number", "the document has no number", document => document with { Document = document.Document with { Number = "" } } },
        { "unreadable number", "number 'D-26-1' is not a quote number", document => document with { Document = document.Document with { Number = "D-26-1" } } },
        { "no issue date", "the issue date '' is not a date", document => document with { Document = document.Document with { IssuedOn = "" } } },
        { "no validity", "a quote has no validity date", document => document with { Document = document.Document with { ValidUntil = null } } },
        { "validity before issue", "the validity does not end after the issue date", document => document with { Document = document.Document with { ValidUntil = "2026-10-18" } } },
        { "unreadable validity", "the validity does not end after the issue date", document => document with { Document = document.Document with { ValidUntil = "17/11/2026" } } },
        { "issuer without name", "the issuer has no name", document => document with { Issuer = document.Issuer with { Name = " " } } },
        { "issuer with a short SIREN", "the issuer's SIREN '91234' is not nine digits", document => document with { Issuer = document.Issuer with { Siren = "91234" } } },
        { "issuer with a SIREN not all digits", "the issuer's SIREN '91234567X' is not nine digits", document => document with { Issuer = document.Issuer with { Siren = "91234567X" } } },
        { "issuer without VAT number", "the issuer has no VAT number", document => document with { Issuer = document.Issuer with { VatNumber = "" } } },
        { "issuer without address", "the issuer has no address", document => document with { Issuer = document.Issuer with { Address = [] } } },
        { "customer without name", "the customer has no name", document => document with { Customer = document.Customer with { Name = "" } } },
        { "customer without address", "the customer has no address", document => document with { Customer = document.Customer with { Address = [] } } },
        { "customer with a SIREN only", "the customer has a SIREN and no VAT number, or the reverse", document => document with { Customer = document.Customer with { VatNumber = null } } },
        { "no line", "the document has no line", document => document with { Lines = [] } },
        { "line without designation", "line 1 has no designation", document => document with { Lines = [document.Lines[0] with { Designation = "" }] } },
        { "line with an unreadable amount", "line 1 has an amount that is not a number", document => document with { Lines = [document.Lines[0] with { TotalExclVat = "1,00" }] } },
        { "line with an unreadable VAT rate", "line 1 has an amount that is not a number", document => document with { Lines = [document.Lines[0] with { VatRate = "dix" }] } },
        { "lines not adding up", "the lines do not add up to the total excluding VAT", document => document with { Lines = [document.Lines[0] with { TotalExclVat = "99.00" }] } },
        { "breakdown base not adding up", "the VAT breakdown does not add up to the totals", document => document with { VatBreakdown = [document.VatBreakdown[0] with { BaseExclVat = "99.00" }] } },
        { "breakdown VAT not adding up", "the VAT breakdown does not add up to the totals", document => document with { VatBreakdown = [document.VatBreakdown[0] with { Vat = "19.00" }] } },
        { "total including VAT not adding up", "the total including VAT is not the total excluding VAT plus the VAT", document => document with { Totals = document.Totals with { InclVat = "121.00" } } },
        { "unreadable total", "a total is not a number", document => document with { Totals = document.Totals with { Vat = "vingt" } } },
        { "no legal identity", "the mention IssuerLegalIdentity is missing", document => document with { Mentions = [.. document.Mentions.Where(mention => mention.Code != "IssuerLegalIdentity")] } },
        { "no validity mention", "the mention QuoteValidity is missing", document => document with { Mentions = [.. document.Mentions.Where(mention => mention.Code != "QuoteValidity")] } },
        { "reduced rate without its mention", "the mention ReducedVatRate is missing", ReducedRateWithoutItsMention },
    };

    [Theory]
    [MemberData(nameof(Defects))]
    public void Should_RefuseTheDocumentAndSayWhy_When_APartIsDefective(string defect, string violation, Func<ResolvedDocument, ResolvedDocument> corrupt)
    {
        var refusal = Record.Exception(() => Validator.Validate(corrupt(ValidDocument())));

        var invalid = Assert.IsType<InvalidDocumentException>(refusal);
        Assert.True(invalid.Violations.Contains(violation), $"A document with {defect} was not refused for: {violation}. Violations: {string.Join(" | ", invalid.Violations)}");
    }

    // Composite: every part speaks, so one refusal lists everything that is wrong.
    [Fact]
    public void Should_ListTheViolationsOfEveryPart_When_SeveralPartsAreDefective()
    {
        var document = ValidDocument();
        var defective = document with { Issuer = document.Issuer with { Name = "" }, Customer = document.Customer with { Name = "" } };

        var refusal = Assert.Throws<InvalidDocumentException>(() => Validator.Validate(defective));

        Assert.Equal(["the issuer has no name", "the customer has no name"], refusal.Violations);
        Assert.Contains("the issuer has no name; the customer has no name", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_AcceptTheInvoice_When_EveryPartIsComplete()
    {
        var refusal = Record.Exception(() => Validator.Validate(ValidInvoice()));

        Assert.Null(refusal);
    }

    public static TheoryData<string, string, Func<ResolvedDocument, ResolvedDocument>> InvoiceDefects => new()
    {
        { "quote number for an invoice", "number 'D-2026-0001' is not an invoice number", document => document with { Document = document.Document with { Number = "D-2026-0001" } } },
        { "no issue date", "the issue date '' is not a date", document => document with { Document = document.Document with { IssuedOn = "" } } },
        { "no number", "the document has no number", document => document with { Document = document.Document with { Number = "" } } },
        { "no quote reference", "an invoice refers to no quote", document => document with { Document = document.Document with { Quote = null } } },
        { "unreadable quote reference", "the invoice refers to 'D-1' dated '', not to a quote", document => document with { Document = document.Document with { Quote = new QuoteReference("D-1", "") } } },
        { "acceptance box", "an invoice has no acceptance box", document => document with { Document = document.Document with { AcceptanceBox = true } } },
        { "no due date mention", "the mention PaymentDueDate is missing", document => document with { Mentions = [.. document.Mentions.Where(mention => mention.Code != "PaymentDueDate")] } },
        { "no recovery indemnity", "the mention RecoveryIndemnity is missing", document => document with { Mentions = [.. document.Mentions.Where(mention => mention.Code != "RecoveryIndemnity")] } },
    };

    [Theory]
    [MemberData(nameof(InvoiceDefects))]
    public void Should_RefuseTheInvoiceAndSayWhy_When_APartIsDefective(string defect, string violation, Func<ResolvedDocument, ResolvedDocument> corrupt)
    {
        var refusal = Record.Exception(() => Validator.Validate(corrupt(ValidInvoice())));

        var invalid = Assert.IsType<InvalidDocumentException>(refusal);
        Assert.True(invalid.Violations.Contains(violation), $"An invoice with {defect} was not refused for: {violation}. Violations: {string.Join(" | ", invalid.Violations)}");
    }

    private static ResolvedDocument ValidInvoice()
    {
        var scope = Scopes.Draft();
        var texts = new FrenchMentionTexts();
        scope.Quote.AddLine(Scopes.Line(vatRate: 20m));
        scope.Quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, texts.IssuerLegalIdentity(scope.Issuer)));
        scope.Quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        var invoice = Invoice.IssueFor(scope.Quote, new InvoiceNumber(2026, 1), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30), new PaymentMentionTexts(
            texts.InvoiceDueDate(new DateOnly(2026, 12, 30)), texts.NoEarlyPaymentDiscount(), texts.LatePaymentPenalties(), texts.RecoveryIndemnity()));
        return ResolvedDocument.OfInvoice(invoice, scope.Issuer, scope.Customer);
    }

    private static ResolvedDocument ValidDocument()
    {
        var scope = Scopes.Draft();
        scope.Quote.AddLine(Scopes.Line(vatRate: 20m));
        scope.Quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, new FrenchMentionTexts().IssuerLegalIdentity(scope.Issuer)));
        scope.Quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return ResolvedDocument.OfQuote(scope);
    }

    // One line at 10 % among others is enough to require the mention.
    private static ResolvedDocument ReducedRateWithoutItsMention(ResolvedDocument document) => document with
    {
        Lines = [document.Lines[0], document.Lines[0] with { VatRate = "10" }],
        VatBreakdown = [document.VatBreakdown[0], new DocumentVatRate("10", "100.00", "10.00")],
        Totals = new DocumentTotals("200.00", "30.00", "230.00"),
    };

    // Each defect is reported once, by its own part: an unknown kind is not also read as a quote, and an
    // unreadable amount is not also reported as totals that do not add up.
    [Fact]
    public void Should_ReportTheKindOnly_When_TheKindIsUnknown()
    {
        var document = ValidDocument();

        var refusal = Assert.Throws<InvalidDocumentException>(() => Validator.Validate(document with { Document = document.Document with { Kind = "letter", Number = "L-1" } }));

        Assert.Equal(["the document kind 'letter' is unknown"], refusal.Violations);
    }

    [Fact]
    public void Should_ReportTheLineOnly_When_ALineAmountIsUnreadable()
    {
        var document = ValidDocument();

        var refusal = Assert.Throws<InvalidDocumentException>(() => Validator.Validate(document with { Lines = [document.Lines[0] with { TotalExclVat = "cent" }] }));

        Assert.Equal(["line 1 has an amount that is not a number"], refusal.Violations);
    }
}
