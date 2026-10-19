namespace InvoiceKata.Domain;

// Strategy (ADR-0002, ADR-0011): how a quote treats VAT, chosen once by its issuer's regime.
internal abstract class VatRegimeStrategy
{
    private static readonly VatRegimeStrategy Normal = new NormalVatRegime();
    private static readonly VatRegimeStrategy Franchise = new VatFranchise();

    public static VatRegimeStrategy Of(VatRegime regime) => regime switch
    {
        VatRegime.Normal => Normal,
        VatRegime.Franchise => Franchise,
        _ => throw new ArgumentOutOfRangeException(nameof(regime), regime, "This VAT regime has no strategy."),
    };

    /// <exception cref="VatRateNotApplicableException">The regime does not take this line's VAT rate.</exception>
    public abstract void Admit(QuoteId quoteId, NewQuoteLine line);

    public abstract IReadOnlyList<VatBreakdownLine> BreakdownOf(IEnumerable<QuoteLine> lines);

    // VAT is computed once per rate, on the total excluding VAT of that rate, never line by line (ADR-0008).
    private sealed class NormalVatRegime : VatRegimeStrategy
    {
        public override void Admit(QuoteId quoteId, NewQuoteLine line)
        {
        }

        public override IReadOnlyList<VatBreakdownLine> BreakdownOf(IEnumerable<QuoteLine> lines) =>
        [.. lines
            .GroupBy(line => line.VatRate)
            .OrderBy(group => group.Key.Percent)
            .Select(group =>
            {
                var baseExcludingVat = group.Aggregate(Money.Zero, (total, line) => total + line.TotalExcludingVat);
                return new VatBreakdownLine(group.Key, baseExcludingVat, group.Key.VatOn(baseExcludingVat));
            })];
    }

    // No VAT at all: a line takes no rate, and there is no breakdown.
    private sealed class VatFranchise : VatRegimeStrategy
    {
        public override void Admit(QuoteId quoteId, NewQuoteLine line)
        {
            if (line.VatRate.Percent != 0m)
            {
                throw new VatRateNotApplicableException(quoteId, line.VatRate);
            }
        }

        public override IReadOnlyList<VatBreakdownLine> BreakdownOf(IEnumerable<QuoteLine> lines) => [];
    }
}
