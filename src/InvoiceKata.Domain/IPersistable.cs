namespace InvoiceKata.Domain;

// A class that can be stored must offer both ways, which the compiler checks (CDR-0006).
public interface IPersistable<TSelf, TState>
    where TSelf : IPersistable<TSelf, TState>
{
    static abstract TSelf FromState(TState state);

    TState ToState();
}

internal static class StateGuard
{
    public static void Require(bool condition, string reason)
    {
        if (!condition)
        {
            throw new ArgumentException($"Invalid stored state: {reason}.", "state");
        }
    }

    public static void RequireVersion(int version, int supported) =>
        Require(version == supported, $"version {version} is not supported, only {supported} is");

    // Issued totals add up by additions only: with no breakdown (VAT franchise, ADR-0011) there is no VAT at all;
    // otherwise the total excluding VAT and the VAT are the sums of the breakdown's. The total including VAT follows.
    public static void RequireTotalsAddUp(
        decimal totalExcludingVat, IReadOnlyList<VatBreakdownState> breakdown, decimal totalVat, decimal totalIncludingVat) =>
        Require(
            (breakdown.Count == 0
                ? totalVat == 0m
                : totalExcludingVat == breakdown.Sum(rate => rate.BaseExcludingVat) && totalVat == breakdown.Sum(rate => rate.Vat))
            && totalIncludingVat == totalExcludingVat + totalVat,
            "the issued totals do not add up");
}
