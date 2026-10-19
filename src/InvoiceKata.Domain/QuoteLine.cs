namespace InvoiceKata.Domain;

// What a caller provides to add a line; required init properties, so no two values can be swapped.
public sealed record NewQuoteLine
{
    public required string Designation { get; init; }

    public required Quantity Quantity { get; init; }

    public required string Unit { get; init; }

    public required Money UnitPriceExcludingVat { get; init; }

    public required VatRate VatRate { get; init; }

    public DiscountRate Discount { get; init; } = DiscountRate.None;
}

public sealed partial class QuoteLine
{
    internal QuoteLine(QuoteLineId id, NewQuoteLine line)
    {
        Id = id;
        Designation = line.Designation;
        Quantity = line.Quantity;
        Unit = line.Unit;
        UnitPriceExcludingVat = line.UnitPriceExcludingVat;
        VatRate = line.VatRate;
        Discount = line.Discount;
    }

    public QuoteLineId Id { get; }

    public string Designation { get; }

    public Quantity Quantity { get; }

    public string Unit { get; }

    public Money UnitPriceExcludingVat { get; }

    public VatRate VatRate { get; }

    public DiscountRate Discount { get; }

    public Money NetUnitPriceExcludingVat => Discount.AppliedTo(UnitPriceExcludingVat);

    public Money TotalExcludingVat => new Money(Quantity.Value * NetUnitPriceExcludingVat.Amount).RoundedToTheCent();
}

public sealed record VatBreakdownLine(VatRate Rate, Money BaseExcludingVat, Money Vat);
