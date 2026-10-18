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
        { "issuer without name", "the issuer has no name", document => document with { Issuer = document.Issuer with { Name = " " } } },
        { "issuer with a short SIREN", "the issuer's SIREN '91234' is not nine digits", document => document with { Issuer = document.Issuer with { Siren = "91234" } } },
        { "issuer without VAT number", "the issuer has no VAT number", document => document with { Issuer = document.Issuer with { VatNumber = "" } } },
        { "issuer without address", "the issuer has no address", document => document with { Issuer = document.Issuer with { Address = [] } } },
        { "customer without name", "the customer has no name", document => document with { Customer = document.Customer with { Name = "" } } },
        { "customer without address", "the customer has no address", document => document with { Customer = document.Customer with { Address = [] } } },
        { "customer with a SIREN only", "the customer has a SIREN and no VAT number, or the reverse", document => document with { Customer = document.Customer with { VatNumber = null } } },
        { "no line", "the document has no line", document => document with { Lines = [] } },
        { "line without designation", "line 1 has no designation", document => document with { Lines = [document.Lines[0] with { Designation = "" }] } },
        { "line with an unreadable amount", "line 1 has an amount that is not a number", document => document with { Lines = [document.Lines[0] with { TotalExclVat = "1,00" }] } },
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

    private static ResolvedDocument ValidDocument()
    {
        var scope = Scopes.Draft();
        scope.Quote.AddLine(Scopes.Line(vatRate: 20m));
        scope.Quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, new FrenchMentionTexts().IssuerLegalIdentity(scope.Issuer)));
        scope.Quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return ResolvedDocument.OfQuote(scope);
    }

    private static ResolvedDocument ReducedRateWithoutItsMention(ResolvedDocument document) => document with
    {
        Lines = [document.Lines[0] with { VatRate = "10", TotalExclVat = "100.00" }],
        VatBreakdown = [new DocumentVatRate("10", "100.00", "10.00")],
        Totals = new DocumentTotals("100.00", "10.00", "110.00"),
    };
}
