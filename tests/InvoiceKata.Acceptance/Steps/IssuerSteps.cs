using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

using Reqnroll;

namespace InvoiceKata.Acceptance.Steps;

[Binding]
public sealed class IssuerSteps(AcceptanceContext context)
{
    private ConfigureIssuerCommand? _command;
    private IssuerId _issuerId;

    [When("je configure l'émetteur de type société suivant :")]
    public async Task WhenIConfigureACompanyIssuer(DataTable table)
    {
        var fields = table.Rows.ToDictionary(row => row["champ"], row => row["valeur"]);
        _command = new ConfigureIssuerCommand(
            Name: fields["dénomination"],
            LegalForm: fields["forme juridique"],
            ShareCapital: decimal.Parse(fields["capital"], System.Globalization.CultureInfo.InvariantCulture),
            Siren: new Siren(fields["SIREN"]),
            VatNumber: new VatNumber(fields["numéro de TVA"]),
            Address: new Address { Street = fields["adresse"], PostalCode = fields["code postal"], City = fields["ville"] });
        _issuerId = await context.ConfigureIssuer.HandleAsync(_command, CancellationToken.None);
    }

    [Then("l'émetteur est configuré avec ces informations")]
    public async Task ThenTheIssuerIsConfiguredWithTheseDetails()
    {
        var issuer = await context.GetIssuer.HandleAsync(_issuerId, CancellationToken.None);

        Assert.Equal(_command!.Name, issuer.Name);
        Assert.Equal(_command.LegalForm, issuer.LegalForm);
        Assert.Equal(_command.ShareCapital, issuer.ShareCapital);
        Assert.Equal(_command.Siren, issuer.Siren);
        Assert.Equal(_command.VatNumber, issuer.VatNumber);
        Assert.Equal(_command.Address, issuer.Address);
    }
}
