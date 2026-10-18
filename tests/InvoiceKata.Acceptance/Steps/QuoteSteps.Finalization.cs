using System.Globalization;

using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

using Reqnroll;

namespace InvoiceKata.Acceptance.Steps;

public sealed partial class QuoteSteps
{
    [Given(@"^nous sommes le (\d{2}/\d{2}/\d{4})$")]
    public void GivenTodayIs(string today) => context.Clock.SetToday(Date(today));

    [When("je finalise le devis")]
    [Given("le devis est finalisé")]
    public Task WhenIFinalizeTheQuote() => FinalizeAsync(_quoteId, FinalizeQuote.DefaultValidityDays);

    [When("je finalise le devis pour une validité de {int} jours")]
    public Task WhenIFinalizeTheQuoteFor(int validityDays) => FinalizeAsync(_quoteId, validityDays);

    [When("j'essaie de finaliser le devis")]
    public async Task WhenITryToFinalizeTheQuote() =>
        context.LastFailure = await Record.ExceptionAsync(WhenIFinalizeTheQuote);

    [Given("{string} a déjà finalisé {int} devis pour {string}")]
    public async Task GivenTheIssuerAlreadyFinalizedQuotes(string issuer, int count, string customer)
    {
        var issuerId = context.IssuersByName[issuer];
        for (var finalized = 0; finalized < count; finalized++)
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
            await context.FinalizeQuote.HandleAsync(issuerId, quoteId, FinalizeQuote.DefaultValidityDays, CancellationToken.None);
        }
    }

    [When("j'essaie d'ajouter au devis la ligne suivante :")]
    public async Task WhenITryToAddTheFollowingLine(DataTable table) =>
        context.LastFailure = await Record.ExceptionAsync(() => WhenIAddTheFollowingLines(table));

    [When("j'essaie de retirer la ligne {string} du devis")]
    public async Task WhenITryToRemoveTheLine(string designation) =>
        context.LastFailure = await Record.ExceptionAsync(() => WhenIRemoveTheLine(designation));

    [Then(@"^le devis est finalisé sous le numéro ""(.*)"", daté du (\d{2}/\d{2}/\d{4}) et valable jusqu'au (\d{2}/\d{2}/\d{4})$")]
    public async Task ThenTheQuoteIsFinalized(string number, string issuedOn, string validUntil)
    {
        var quote = await QuoteAsync();

        Assert.Equal(
            (QuoteStatus.Finalized, (QuoteNumber?)QuoteNumber.Parse(number), (DateOnly?)Date(issuedOn), (DateOnly?)Date(validUntil)),
            (quote.Status, quote.Number, quote.IssuedOn, quote.ValidUntil));
    }

    [Then("la modification est refusée, car le devis est finalisé")]
    public void ThenTheChangeIsRefusedAsTheQuoteIsFinalized() => Assert.IsType<QuoteAlreadyFinalizedException>(context.LastFailure);

    [Then("la finalisation est refusée, car le devis n'a aucune ligne")]
    public void ThenTheFinalizationIsRefusedAsTheQuoteHasNoLine() => Assert.IsType<QuoteWithoutLinesException>(context.LastFailure);

    private Task FinalizeAsync(QuoteId quoteId, int validityDays) =>
        context.FinalizeQuote.HandleAsync(_issuerId, quoteId, validityDays, CancellationToken.None);

    private static DateOnly Date(string text) => DateOnly.ParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
}
