using InvoiceKata.Domain;

using Reqnroll;

using UglyToad.PdfPig;

namespace InvoiceKata.Acceptance.Steps;

public sealed partial class QuoteSteps
{
    private byte[] _pdfBeforeReplay = [];

    [Then("le PDF du devis contient :")]
    public async Task ThenTheQuotePdfContains(DataTable table)
    {
        using var pdf = PdfDocument.Open(await PdfAsync());
        var text = Flat(string.Concat(pdf.GetPages().Select(page => page.Text)));

        Assert.All(table.Rows.Select(row => row["texte"]), fragment => Assert.Contains(Flat(fragment), text, StringComparison.Ordinal));
    }

    [When("l'événement de finalisation du devis est rejoué")]
    public async Task WhenTheQuoteFinalizationIsReplayed()
    {
        _pdfBeforeReplay = await PdfAsync();
        var quote = await QuoteAsync();

        await context.QuoteReactions.PublishAsync([new QuoteFinalized(quote.Id, quote.IssuerId, quote.Number!.Value)], CancellationToken.None);
    }

    [Then("le PDF du devis n'a pas changé")]
    public async Task ThenTheQuotePdfDidNotChange() => Assert.Equal(_pdfBeforeReplay, await PdfAsync());

    [When("j'essaie d'obtenir le PDF du devis")]
    public async Task WhenITryToGetTheQuotePdf() => context.LastFailure = await Record.ExceptionAsync(PdfAsync);

    [Then("la demande est refusée, car le devis n'est pas encore finalisé")]
    public void ThenTheRequestIsRefusedAsTheQuoteIsADraft() => Assert.IsType<QuoteNotFinalizedException>(context.LastFailure);

    private Task<byte[]> PdfAsync() => context.RenderQuotePdf.PdfOfAsync(_issuerId, _quoteId, CancellationToken.None);

    // The PDF spaces words and thousands its own way, and Typst curls apostrophes: compare without either.
    private static string Flat(string text) => string.Concat(text.Where(character => !char.IsWhiteSpace(character))).Replace('’', '\'');
}
