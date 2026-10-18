using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

using Reqnroll;

namespace InvoiceKata.Acceptance.Steps;

[Binding]
public sealed class CustomerSteps(AcceptanceContext context)
{
    private RegisterCustomerCommand? _command;
    private CustomerId _customerId;

    [Given("l'émetteur de type société {string} configuré")]
    public async Task GivenACompanyIssuer(string name)
    {
        var command = new ConfigureIssuerCommand(
            name, LegalForm.Sas, 20000m, new Siren("912345678"), new VatNumber("FR12912345678"),
            new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });
        context.IssuersByName[name] = await context.ConfigureIssuer.HandleAsync(command, CancellationToken.None);
    }

    [When("{string} enregistre le client professionnel suivant :")]
    public async Task WhenTheIssuerRegistersAProfessionalCustomer(string issuer, DataTable table)
    {
        var fields = table.Rows.ToDictionary(row => row["champ"], row => row["valeur"]);
        await RegisterAsync(issuer, new RegisterCustomerCommand(
            fields["nom"],
            new Siren(fields["SIREN"]),
            new VatNumber(fields["numéro de TVA"]),
            new Address { Street = fields["adresse"], PostalCode = fields["code postal"], City = fields["ville"] }));
    }

    [Given("{string} a enregistré le client professionnel {string}")]
    public Task GivenTheIssuerRegisteredAProfessionalCustomer(string issuer, string name) =>
        RegisterAsync(issuer, new RegisterCustomerCommand(
            name, new Siren("834567890"), new VatNumber("FR45834567890"),
            new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" }));

    [Then("{string} retrouve ce client professionnel avec ces informations")]
    public async Task ThenTheIssuerFindsThisProfessionalCustomer(string issuer)
    {
        var customer = await context.GetCustomer.HandleAsync(context.IssuersByName[issuer], _customerId, CancellationToken.None);

        Assert.Equal(CustomerKind.Professional, customer.Kind);
        Assert.Equal(_command!.Name, customer.Name);
        Assert.Equal(_command.Siren, customer.Siren);
        Assert.Equal(_command.VatNumber, customer.VatNumber);
        Assert.Equal(_command.Address, customer.Address);
    }

    [When("{string} cherche ce client")]
    public async Task WhenTheIssuerLooksForThisCustomer(string issuer)
    {
        context.LastFailure = await Record.ExceptionAsync(() =>
            context.GetCustomer.HandleAsync(context.IssuersByName[issuer], _customerId, CancellationToken.None));
    }

    [Then("le client est introuvable")]
    public void ThenTheCustomerIsNotFound() => Assert.IsType<CustomerNotFoundException>(context.LastFailure);

    private async Task RegisterAsync(string issuer, RegisterCustomerCommand command)
    {
        _command = command;
        _customerId = await context.RegisterCustomer.HandleAsync(context.IssuersByName[issuer], command, CancellationToken.None);
        context.CustomersByName[command.Name] = _customerId;
    }
}
