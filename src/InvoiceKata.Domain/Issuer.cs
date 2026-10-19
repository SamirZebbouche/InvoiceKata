namespace InvoiceKata.Domain;

// Who issues quotes and invoices; one SaaS instance serves many of them (ADR-0007). A company, at the normal VAT
// regime, or an individual entrepreneur, in VAT franchise, without legal form, capital nor VAT number (ADR-0011).
public sealed partial class Issuer
{
    private Issuer(
        IssuerId id, IssuerKind kind, VatRegime vatRegime, string name, LegalForm? legalForm, decimal? shareCapital, Siren siren, VatNumber? vatNumber,
        Address address)
    {
        Id = id;
        Kind = kind;
        VatRegime = vatRegime;
        Name = name;
        LegalForm = legalForm;
        ShareCapital = shareCapital;
        Siren = siren;
        VatNumber = vatNumber;
        Address = address;
    }

    public IssuerId Id { get; }

    public IssuerKind Kind { get; }

    public VatRegime VatRegime { get; }

    public string Name { get; }

    // A company's only.
    public LegalForm? LegalForm { get; }

    // A company's only.
    public decimal? ShareCapital { get; }

    public Siren Siren { get; }

    // A company's only: an individual entrepreneur in franchise bills no VAT.
    public VatNumber? VatNumber { get; }

    public Address Address { get; }

    public static Issuer ConfigureCompany(string name, LegalForm legalForm, decimal shareCapital, Siren siren, VatNumber vatNumber, Address address) =>
        new(IssuerId.New(), IssuerKind.Company, VatRegime.Normal, name, legalForm, shareCapital, siren, vatNumber, address);

    public static Issuer ConfigureIndividualEntrepreneur(string name, Siren siren, Address address) =>
        new(IssuerId.New(), IssuerKind.IndividualEntrepreneur, VatRegime.Franchise, name, null, null, siren, null, address);
}
