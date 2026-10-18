namespace InvoiceKata.Domain;

public enum CustomerKind
{
    Professional,
}

// A customer belongs to exactly one issuer (ADR-0007).
public sealed class Customer
{
    private Customer(CustomerId id, IssuerId issuerId, CustomerKind kind, string name, string siren, string vatNumber, Address address)
    {
        Id = id;
        IssuerId = issuerId;
        Kind = kind;
        Name = name;
        Siren = siren;
        VatNumber = vatNumber;
        Address = address;
    }

    public CustomerId Id { get; }

    public IssuerId IssuerId { get; }

    public CustomerKind Kind { get; }

    public string Name { get; }

    public string Siren { get; }

    public string VatNumber { get; }

    public Address Address { get; }

    public static Customer RegisterProfessional(IssuerId issuerId, string name, string siren, string vatNumber, Address address) =>
        new(CustomerId.New(), issuerId, CustomerKind.Professional, name, siren, vatNumber, address);
}
