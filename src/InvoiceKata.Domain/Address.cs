namespace InvoiceKata.Domain;

// Required init properties: every call site names each part, so street, postal code and city cannot be swapped.
public sealed record Address
{
    public required string Street { get; init; }

    public required string PostalCode { get; init; }

    public required string City { get; init; }
}
