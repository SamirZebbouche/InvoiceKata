namespace InvoiceKata.Domain;

// A company that issues quotes and invoices; one SaaS instance serves many of them (ADR-0007).
public sealed class Issuer
{
    private Issuer(IssuerId id, string name, string legalForm, decimal shareCapital, string siren, string vatNumber, Address address)
    {
        Id = id;
        Name = name;
        LegalForm = legalForm;
        ShareCapital = shareCapital;
        Siren = siren;
        VatNumber = vatNumber;
        Address = address;
    }

    public IssuerId Id { get; }

    public string Name { get; }

    public string LegalForm { get; }

    public decimal ShareCapital { get; }

    public string Siren { get; }

    public string VatNumber { get; }

    public Address Address { get; }

    public static Issuer ConfigureCompany(string name, string legalForm, decimal shareCapital, string siren, string vatNumber, Address address) =>
        new(IssuerId.New(), name, legalForm, shareCapital, siren, vatNumber, address);
}
