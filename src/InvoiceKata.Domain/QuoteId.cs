namespace InvoiceKata.Domain;

public readonly record struct QuoteId(Guid Value)
{
    // Random v4, never v7, so it cannot be guessed (ADR-0007).
    public static QuoteId New() => new(Guid.NewGuid());
}
