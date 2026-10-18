namespace InvoiceKata.Domain;

public readonly record struct IssuerId(Guid Value)
{
    // Random v4, not v7: a v7 starts with a timestamp, which leaks the creation date and narrows guessing (ADR-0007).
    public static IssuerId New() => new(Guid.NewGuid());
}
