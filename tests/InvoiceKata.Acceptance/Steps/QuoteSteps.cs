using InvoiceKata.Domain;

using Reqnroll;

namespace InvoiceKata.Acceptance.Steps;

[Binding]
public sealed class QuoteSteps(AcceptanceContext context)
{
    private IssuerId _issuerId;
    private QuoteId _quoteId;

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
}
