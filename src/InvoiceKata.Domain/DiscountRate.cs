namespace InvoiceKata.Domain;

// A discount in percent (10 for 10 %). A type of its own so it cannot be swapped with a VAT rate.
public readonly record struct DiscountRate(decimal Percent)
{
    public static DiscountRate None { get; } = new(0m);

    // The net price is an input, not a legal amount: it stays at full precision (ADR-0008).
    public Money AppliedTo(Money unitPrice) => new(unitPrice.Amount * (1m - Percent / 100m));
}
