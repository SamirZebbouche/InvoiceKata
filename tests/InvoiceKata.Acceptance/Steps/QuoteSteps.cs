using System.Globalization;

using InvoiceKata.Domain;

using Reqnroll;

namespace InvoiceKata.Acceptance.Steps;

[Binding]
public sealed class QuoteSteps(AcceptanceContext context)
{
    private IssuerId _issuerId;
    private QuoteId _quoteId;
    private readonly Dictionary<string, QuoteLineId> _lineIds = [];

    [When("{string} crée un devis pour {string}")]
    public async Task WhenTheIssuerCreatesAQuoteFor(string issuer, string customer)
    {
        _issuerId = context.IssuersByName[issuer];
        context.LastFailure = await Record.ExceptionAsync(async () =>
            _quoteId = await context.CreateQuote.HandleAsync(_issuerId, context.CustomersByName[customer], CancellationToken.None));
    }

    [Then("le devis est un brouillon sans ligne, établi pour {string}")]
    public async Task ThenTheQuoteIsAnEmptyDraftFor(string customer)
    {
        Assert.Null(context.LastFailure);
        var quote = await context.GetQuote.HandleAsync(_issuerId, _quoteId, CancellationToken.None);

        Assert.Equal(QuoteStatus.Draft, quote.Status);
        Assert.Empty(quote.Lines);
        Assert.Equal(context.CustomersByName[customer], quote.CustomerId);
    }

    [Given("un devis brouillon de {string} pour {string}")]
    public Task GivenADraftQuote(string issuer, string customer) => WhenTheIssuerCreatesAQuoteFor(issuer, customer);

    [When("j'ajoute au devis la ligne suivante :")]
    [Given("le devis contient les lignes suivantes :")]
    public async Task WhenIAddTheFollowingLines(DataTable table)
    {
        foreach (var row in table.Rows)
        {
            var line = new NewQuoteLine
            {
                Designation = row["désignation"],
                Quantity = new Quantity(Amount(row["quantité"])),
                Unit = row["unité"],
                UnitPriceExcludingVat = new Money(Amount(row["prix unitaire HT"])),
                VatRate = new VatRate(Amount(row["taux de TVA"])),
                Discount = table.ContainsColumn("remise") ? new DiscountRate(Amount(row["remise"])) : DiscountRate.None,
            };
            _lineIds[line.Designation] = await context.AddQuoteLine.HandleAsync(_issuerId, _quoteId, line, CancellationToken.None);
        }
    }

    [When("je retire la ligne {string} du devis")]
    public Task WhenIRemoveTheLine(string designation) =>
        context.RemoveQuoteLine.HandleAsync(_issuerId, _quoteId, _lineIds[designation], CancellationToken.None);

    [Then("le devis contient {int} ligne(s)")]
    public async Task ThenTheQuoteHasLines(int count) => Assert.Equal(count, (await QuoteAsync()).Lines.Count);

    [Then("les totaux du devis sont :")]
    public async Task ThenTheQuoteTotalsAre(DataTable table)
    {
        var expected = table.Rows.Single();
        var quote = await QuoteAsync();

        Assert.Equal(new Money(Amount(expected["total HT"])), quote.TotalExcludingVat);
        Assert.Equal(new Money(Amount(expected["total TVA"])), quote.TotalVat);
        Assert.Equal(new Money(Amount(expected["total TTC"])), quote.TotalIncludingVat);
    }

    [Then("la TVA du devis est ventilée ainsi :")]
    public async Task ThenTheVatIsBrokenDownAs(DataTable table)
    {
        var expected = table.Rows.Select(row => new VatBreakdownLine(
            new VatRate(Amount(row["taux"])), new Money(Amount(row["base HT"])), new Money(Amount(row["TVA"]))));

        Assert.Equal(expected, (await QuoteAsync()).VatBreakdown);
    }

    private Task<Quote> QuoteAsync() => context.GetQuote.HandleAsync(_issuerId, _quoteId, CancellationToken.None);

    // Scenarios write amounts the French way ("1600,00").
    private static decimal Amount(string text) => decimal.Parse(text, CultureInfo.GetCultureInfo("fr-FR"));
}
