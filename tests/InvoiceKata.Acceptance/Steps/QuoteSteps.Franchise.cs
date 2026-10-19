using InvoiceKata.Domain;

using Reqnroll;

namespace InvoiceKata.Acceptance.Steps;

public sealed partial class QuoteSteps
{
    [Then("la ligne est refusée, car le devis est en franchise de TVA")]
    public void ThenTheLineIsRefusedAsTheQuoteIsInFranchise() => Assert.IsType<VatRateNotApplicableException>(context.LastFailure);
}
