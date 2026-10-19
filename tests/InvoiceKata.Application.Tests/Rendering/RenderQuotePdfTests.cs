using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Rendering;

public sealed class RenderQuotePdfTests
{
    private readonly QuoteScope _scope = Scopes.Draft();
    private readonly StoredQuotes _quotes = new();
    private readonly StoredPdfs _pdfs = new();
    private readonly RecordingRenderer _renderer = new();

    [Fact]
    public async Task Should_RenderAndStoreTheValidatedPdf_When_AQuoteIsFinalized()
    {
        var finalized = await FinalizedAsync();

        await Reaction().ReactAsync(finalized, CancellationToken.None);

        Assert.Equal("D-2026-0001", Assert.Single(_renderer.Rendered).Document.Number);
        Assert.Equal("PDF D-2026-0001 #1"u8.ToArray(), await _pdfs.FindAsync(_scope.Issuer.Id, DocumentReference.Of(_scope.Quote.Id), CancellationToken.None));
    }

    // The replay test of the reaction (ADR-0003): handling QuoteFinalized again renders nothing more.
    [Fact]
    public async Task Should_KeepTheFirstPdfAndRenderOnce_When_QuoteFinalizedIsHandledAgain()
    {
        var finalized = await FinalizedAsync();
        var reaction = Reaction();
        await reaction.ReactAsync(finalized, CancellationToken.None);

        await reaction.ReactAsync(finalized, CancellationToken.None);

        Assert.Single(_renderer.Rendered);
        Assert.Equal("PDF D-2026-0001 #1"u8.ToArray(), await _pdfs.FindAsync(_scope.Issuer.Id, DocumentReference.Of(_scope.Quote.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Should_DoNothing_When_TheEventIsNotAFinalization()
    {
        await Reaction().ReactAsync(new QuoteDrafted(_scope.Quote.Id), CancellationToken.None);

        Assert.Empty(_renderer.Rendered);
    }

    // No PDF from a document the final validator refuses (ADR-0005, decision 9).
    [Fact]
    public async Task Should_RenderAndStoreNothing_When_TheDocumentIsInvalid()
    {
        var finalized = await FinalizedAsync(withLegalIdentity: false);

        var refusal = await Record.ExceptionAsync(() => Reaction().ReactAsync(finalized, CancellationToken.None));

        Assert.Contains("the mention IssuerLegalIdentity is missing", Assert.IsType<InvalidDocumentException>(refusal).Violations);
        Assert.Empty(_renderer.Rendered);
        Assert.Null(await _pdfs.FindAsync(_scope.Issuer.Id, DocumentReference.Of(_scope.Quote.Id), CancellationToken.None));
    }

    // Reading the PDF renders it if the reaction failed after the save: the quote stays finalized either way.
    [Fact]
    public async Task Should_RenderTheMissingPdf_When_ItIsAskedFor()
    {
        await FinalizedAsync();

        var pdf = await Reaction().PdfOfAsync(_scope.Issuer.Id, _scope.Quote.Id, CancellationToken.None);

        Assert.Equal("PDF D-2026-0001 #1"u8.ToArray(), pdf);
        Assert.Equal(pdf, await _pdfs.FindAsync(_scope.Issuer.Id, DocumentReference.Of(_scope.Quote.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Should_RefuseToGiveAPdf_When_TheQuoteIsADraft()
    {
        _scope.Quote.AddLine(Scopes.Line(vatRate: 20m));
        await _quotes.AddAsync(_scope.Quote, CancellationToken.None);

        await Assert.ThrowsAsync<QuoteNotFinalizedException>(() => Reaction().PdfOfAsync(_scope.Issuer.Id, _scope.Quote.Id, CancellationToken.None));
        Assert.Empty(_renderer.Rendered);
    }

    private RenderQuotePdf Reaction() => new(
        _quotes,
        new QuoteScopes(new StoredIssuers(_scope.Issuer), new StoredCustomers(_scope.Customer)),
        new FinalDocumentValidator([new HeaderValidator(), new IssuerValidator(), new CustomerValidator(), new LinesValidator(), new TotalsValidator(), new MentionsValidator()]),
        _renderer,
        _pdfs);

    private async Task<QuoteFinalized> FinalizedAsync(bool withLegalIdentity = true)
    {
        var quote = _scope.Quote;
        quote.AddLine(Scopes.Line(vatRate: 20m));
        if (withLegalIdentity)
        {
            quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, new FrenchMentionTexts().IssuerLegalIdentity(_scope.Issuer)));
        }

        quote.TakeEvents();
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        await _quotes.AddAsync(quote, CancellationToken.None);
        return Assert.IsType<QuoteFinalized>(Assert.Single(quote.TakeEvents()));
    }
}
