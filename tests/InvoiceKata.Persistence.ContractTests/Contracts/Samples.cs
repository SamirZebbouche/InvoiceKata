using InvoiceKata.Domain;

namespace InvoiceKata.Persistence.ContractTests.Contracts;

// Real domain objects for the contract tests (never mocks of the domain).
internal static class Samples
{
    public static Issuer Issuer() => InvoiceKata.Domain.Issuer.ConfigureCompany(
        "Batifolio", LegalForm.Sas, 20000m, new Siren("912345678"), new VatNumber("FR12912345678"),
        new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });

    public static Customer ProfessionalOf(Issuer issuer) => Customer.RegisterProfessional(
        issuer.Id, "Les Toits Rouges SARL", new Siren("834567890"), new VatNumber("FR45834567890"),
        new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" });

    public static Customer IndividualOf(Issuer issuer) => Customer.RegisterIndividual(
        issuer.Id, "Jeanne Martin", new Address { Street = "3 rue des Lilas", PostalCode = "33000", City = "Bordeaux" });

    public static NewQuoteLine Line(string designation, decimal unitPrice, decimal vatRate, decimal discount = 0m) => new()
    {
        Designation = designation,
        Quantity = new Quantity(18.72m),
        Unit = "m²",
        UnitPriceExcludingVat = new Money(unitPrice),
        VatRate = new VatRate(vatRate),
        Discount = new DiscountRate(discount),
    };
}
