namespace InvoiceKata.Domain;

public enum CustomerKind
{
    Professional,
    Individual,
}

// A customer belongs to exactly one issuer (ADR-0007).
public sealed partial class Customer
{
    private Customer(CustomerId id, IssuerId issuerId, CustomerKind kind, string name, Siren? siren, VatNumber? vatNumber, Address address)
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

    // A professional has both; an individual has neither.
    public Siren? Siren { get; }

    public VatNumber? VatNumber { get; }

    public Address Address { get; }

    public static Customer RegisterProfessional(IssuerId issuerId, string name, Siren siren, VatNumber vatNumber, Address address) =>
        new(CustomerId.New(), issuerId, CustomerKind.Professional, name, siren, vatNumber, address);

    public static Customer RegisterIndividual(IssuerId issuerId, string name, Address address) =>
        new(CustomerId.New(), issuerId, CustomerKind.Individual, name, null, null, address);
}
