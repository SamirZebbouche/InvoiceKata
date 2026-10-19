using InvoiceKata.Application.Invoices;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Rendering;

public sealed class RenderInvoicePdfTests
{
    private readonly QuoteScope _scope = Scopes.Draft();
    private readonly StoredInvoices _invoices = new();
    private readonly StoredPdfs _pdfs = new();
    private readonly RecordingRenderer _renderer = new();

    [Fact]
    public async Task Should_RenderAndStoreTheValidatedPdf_When_AnInvoiceIsIssued()
    {
        var issued = await IssuedAsync();

        await Reaction().ReactAsync(issued, CancellationToken.None);

        Assert.Equal("F-2026-0001", Assert.Single(_renderer.Rendered).Document.Number);
        Assert.Equal("PDF F-2026-0001 #1"u8.ToArray(), await _pdfs.FindAsync(_scope.Issuer.Id, DocumentReference.Of(issued.InvoiceId), CancellationToken.None));
    }

    // The replay test of the reaction (ADR-0003): handling InvoiceIssued again renders nothing more.
    [Fact]
    public async Task Should_KeepTheFirstPdfAndRenderOnce_When_InvoiceIssuedIsHandledAgain()
    {
        var issued = await IssuedAsync();
        var reaction = Reaction();
        await reaction.ReactAsync(issued, CancellationToken.None);

        await reaction.ReactAsync(issued, CancellationToken.None);

        Assert.Single(_renderer.Rendered);
    }

    // Reading the PDF renders it if the reaction failed after the add: the invoice stays issued either way.
    [Fact]
    public async Task Should_RenderTheMissingPdf_When_ItIsAskedFor()
    {
        var issued = await IssuedAsync();

        var pdf = await Reaction().PdfOfAsync(_scope.Issuer.Id, issued.InvoiceId, CancellationToken.None);

        Assert.Equal("PDF F-2026-0001 #1"u8.ToArray(), pdf);
        Assert.Equal(pdf, await _pdfs.FindAsync(_scope.Issuer.Id, DocumentReference.Of(issued.InvoiceId), CancellationToken.None));
    }

    // No PDF from a document the final validator refuses (ADR-0005, decision 9).
    [Fact]
    public async Task Should_RenderAndStoreNothing_When_TheDocumentIsInvalid()
    {
        var issued = await IssuedAsync(withLegalIdentity: false);

        var refusal = await Record.ExceptionAsync(() => Reaction().ReactAsync(issued, CancellationToken.None));

        Assert.Contains("the mention IssuerLegalIdentity is missing", Assert.IsType<InvalidDocumentException>(refusal).Violations);
        Assert.Empty(_renderer.Rendered);
    }

    private RenderInvoicePdf Reaction() => new(
        _invoices,
        new StoredIssuers(_scope.Issuer),
        new StoredCustomers(_scope.Customer),
        new FinalDocumentValidator([new HeaderValidator(), new IssuerValidator(), new CustomerValidator(), new LinesValidator(), new TotalsValidator(), new MentionsValidator()]),
        _renderer,
        _pdfs);

    private async Task<InvoiceIssued> IssuedAsync(bool withLegalIdentity = true)
    {
        var texts = new FrenchMentionTexts();
        var quote = _scope.Quote;
        quote.AddLine(Scopes.Line(vatRate: 20m));
        if (withLegalIdentity)
        {
            quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, texts.IssuerLegalIdentity(_scope.Issuer)));
        }

        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        var invoice = Invoice.IssueFor(quote, new InvoiceNumber(2026, 1), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30), new PaymentMentionTexts(
            texts.InvoiceDueDate(new DateOnly(2026, 12, 30)), texts.NoEarlyPaymentDiscount(), texts.LatePaymentPenalties(), texts.RecoveryIndemnity()));
        await _invoices.AddAsync(invoice, CancellationToken.None);
        return Assert.IsType<InvoiceIssued>(Assert.Single(invoice.TakeEvents()));
    }
}
