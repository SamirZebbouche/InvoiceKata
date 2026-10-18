namespace InvoiceKata.Domain.Tests;

// Real domain objects for the tests (never mocks of the domain).
internal static class Quotes
{
    public static Quote Draft()
    {
        var issuer = Issuer.ConfigureCompany(
            "Batifolio", LegalForm.Sas, 20000m, new Siren("912345678"), new VatNumber("FR12912345678"),
            new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });
        var customer = Customer.RegisterProfessional(
            issuer.Id, "Les Toits Rouges SARL", new Siren("834567890"), new VatNumber("FR45834567890"),
            new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" });
        return Quote.DraftFor(customer);
    }

    public static NewQuoteLine Line(decimal unitPrice, decimal quantity = 1m, decimal vatRate = 20m) => new()
    {
        Designation = "Prestation",
        Quantity = new Quantity(quantity),
        Unit = "u",
        UnitPriceExcludingVat = new Money(unitPrice),
        VatRate = new VatRate(vatRate),
    };
}
