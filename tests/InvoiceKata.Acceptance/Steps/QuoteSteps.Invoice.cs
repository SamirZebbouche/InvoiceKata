using System.Globalization;

using InvoiceKata.Application.Invoices;
using InvoiceKata.Domain;

using Reqnroll;

namespace InvoiceKata.Acceptance.Steps;

public sealed partial class QuoteSteps
{
    private InvoiceId _invoiceId;

    [When("j'émets la facture du devis")]
    [Given("la facture du devis est émise")]
    public async Task WhenIIssueTheInvoice() =>
        _invoiceId = await context.IssueInvoice.HandleAsync(_issuerId, _quoteId, IssueInvoice.DefaultPaymentDays, CancellationToken.None);

    [When("j'émets la facture du devis, payable sous {int} jours")]
    public async Task WhenIIssueTheInvoicePayableWithin(int paymentDays) =>
        _invoiceId = await context.IssueInvoice.HandleAsync(_issuerId, _quoteId, paymentDays, CancellationToken.None);

    [When("j'essaie d'émettre la facture du devis")]
    public async Task WhenITryToIssueTheInvoice() => context.LastFailure = await Record.ExceptionAsync(WhenIIssueTheInvoice);

    [Given("{string} a déjà facturé {int} devis pour {string}")]
    public async Task GivenTheIssuerAlreadyInvoicedQuotes(string issuer, int count, string customer)
    {
        var issuerId = context.IssuersByName[issuer];
        for (var invoiced = 0; invoiced < count; invoiced++)
        {
            var quoteId = await context.CreateQuote.HandleAsync(issuerId, context.CustomersByName[customer], CancellationToken.None);
            await context.AddQuoteLine.HandleAsync(issuerId, quoteId, new NewQuoteLine
            {
                Designation = "Visite",
                Quantity = new Quantity(1m),
                Unit = "forfait",
                UnitPriceExcludingVat = new Money(100m),
                VatRate = new VatRate(20m),
            }, CancellationToken.None);
            await context.FinalizeQuote.HandleAsync(issuerId, quoteId, 30, CancellationToken.None);
            await context.IssueInvoice.HandleAsync(issuerId, quoteId, IssueInvoice.DefaultPaymentDays, CancellationToken.None);
        }
    }

    [Then(@"^la facture est émise sous le numéro ""(.*)"", datée du (\d{2}/\d{2}/\d{4}), payable au plus tard le (\d{2}/\d{2}/\d{4}), d'après le devis ""(.*)""$")]
    public async Task ThenTheInvoiceIsIssued(string number, string issuedOn, string dueOn, string quoteNumber)
    {
        var invoice = await InvoiceAsync();

        Assert.Equal(
            (InvoiceNumber.Parse(number), Day(issuedOn), Day(dueOn), QuoteNumber.Parse(quoteNumber)),
            (invoice.Number, invoice.IssuedOn, invoice.DueOn, invoice.QuoteNumber));
    }

    [Then("les totaux de la facture sont :")]
    public async Task ThenTheInvoiceTotalsAre(DataTable table)
    {
        var expected = table.Rows.Single();
        var invoice = await InvoiceAsync();

        Assert.Equal(
            (Money(expected["total HT"]), Money(expected["total TVA"]), Money(expected["total TTC"])),
            (invoice.TotalExcludingVat, invoice.TotalVat, invoice.TotalIncludingVat));
    }

    [Then("la facture porte les mentions suivantes :")]
    public async Task ThenTheInvoiceCarriesTheMentions(DataTable table)
    {
        var texts = (await InvoiceAsync()).Mentions.Select(mention => WithPlainSpaces(mention.Text)).ToList();

        Assert.All(table.Rows.Select(row => row["mention"]), mention => Assert.Contains(mention, texts));
    }

    [Then("la facture ne porte pas de mention contenant {string}")]
    public async Task ThenTheInvoiceCarriesNoMentionContaining(string fragment) =>
        Assert.DoesNotContain((await InvoiceAsync()).Mentions, mention => mention.Text.Contains(fragment, StringComparison.Ordinal));

    [Then("l'émission est refusée, car le devis a déjà sa facture")]
    public void ThenTheIssueIsRefusedAsTheQuoteIsInvoiced() => Assert.IsType<QuoteAlreadyInvoicedException>(context.LastFailure);

    [Then("l'émission est refusée, car le devis n'est pas finalisé")]
    public void ThenTheIssueIsRefusedAsTheQuoteIsADraft() => Assert.IsType<QuoteNotFinalizedException>(context.LastFailure);

    [Then("le PDF de la facture contient :")]
    public async Task ThenTheInvoicePdfContains(DataTable table)
    {
        var text = PdfText(await context.RenderInvoicePdf.PdfOfAsync(_issuerId, _invoiceId, CancellationToken.None));

        Assert.All(table.Rows.Select(row => row["texte"]), fragment => Assert.Contains(Flat(fragment), text, StringComparison.Ordinal));
    }

    [Then("le PDF de la facture ne contient pas {string}")]
    public async Task ThenTheInvoicePdfDoesNotContain(string fragment) =>
        Assert.DoesNotContain(Flat(fragment), PdfText(await context.RenderInvoicePdf.PdfOfAsync(_issuerId, _invoiceId, CancellationToken.None)), StringComparison.Ordinal);

    private static string PdfText(byte[] pdf)
    {
        using var document = UglyToad.PdfPig.PdfDocument.Open(pdf);
        return Flat(string.Concat(document.GetPages().Select(page => page.Text)));
    }

    private Task<Invoice> InvoiceAsync() => context.GetInvoice.HandleAsync(_issuerId, _invoiceId, CancellationToken.None);

    private static DateOnly Day(string text) => DateOnly.ParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static Money Money(string text) => new(decimal.Parse(text, CultureInfo.GetCultureInfo("fr-FR")));
}
