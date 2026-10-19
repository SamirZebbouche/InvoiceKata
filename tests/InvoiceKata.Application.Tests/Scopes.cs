using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests;

// Real domain objects for the tests (never mocks of the domain): a draft quote with its issuer and customer.
internal static class Scopes
{
    public static QuoteScope Draft()
    {
        var issuer = Issuer.ConfigureCompany(
            "Batifolio", LegalForm.Sas, 20000m, new Siren("912345678"), new VatNumber("FR12912345678"),
            new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });
        var customer = Customer.RegisterProfessional(
            issuer.Id, "Les Toits Rouges SARL", new Siren("834567890"), new VatNumber("FR45834567890"),
            new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" });
        return new QuoteScope(Quote.DraftFor(issuer, customer), issuer, customer);
    }

    // A draft of an individual entrepreneur in VAT franchise (ADR-0011).
    public static QuoteScope FranchiseDraft()
    {
        var issuer = Issuer.ConfigureIndividualEntrepreneur(
            "Jean Dupont", new Siren("853123456"), new Address { Street = "8 chemin des Vignes", PostalCode = "47000", City = "Agen" });
        var customer = Customer.RegisterProfessional(
            issuer.Id, "Les Toits Rouges SARL", new Siren("834567890"), new VatNumber("FR45834567890"),
            new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" });
        return new QuoteScope(Quote.DraftFor(issuer, customer), issuer, customer);
    }

    // A draft for an individual customer (ADR-0012).
    public static QuoteScope IndividualDraft()
    {
        var issuer = Draft().Issuer;
        var customer = Customer.RegisterIndividual(
            issuer.Id, "Jeanne Martin", new Address { Street = "3 rue des Lilas", PostalCode = "33000", City = "Bordeaux" });
        return new QuoteScope(Quote.DraftFor(issuer, customer), issuer, customer);
    }

    public static NewQuoteLine Line(decimal vatRate) => new()
    {
        Designation = "Prestation",
        Quantity = new Quantity(1m),
        Unit = "u",
        UnitPriceExcludingVat = new Money(100m),
        VatRate = new VatRate(vatRate),
    };
}
