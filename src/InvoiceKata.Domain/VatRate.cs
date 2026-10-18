namespace InvoiceKata.Domain;

// A VAT rate in percent (20 for 20 %).
public readonly record struct VatRate(decimal Percent)
{
    public Money VatOn(Money baseExcludingVat) => new Money(baseExcludingVat.Amount * Percent / 100m).RoundedToTheCent();
}
