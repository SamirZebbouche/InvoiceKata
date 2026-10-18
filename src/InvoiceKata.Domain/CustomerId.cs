namespace InvoiceKata.Domain;

public readonly record struct CustomerId(Guid Value)
{
    // Random v4, never v7, so it cannot be guessed (ADR-0007).
    public static CustomerId New() => new(Guid.NewGuid());
}
