namespace InvoiceKata.Domain;

public readonly record struct QuoteLineId(Guid Value)
{
    // Random v4, never v7, so it cannot be guessed (ADR-0007).
    public static QuoteLineId New() => new(Guid.NewGuid());
}
