namespace InvoiceKata.Domain;

// What a quote stores: its inputs and its mentions. A draft recomputes its totals and VAT breakdown; a
// finalized quote stores them as issued, with its number and dates (CDR-0006, version 2).
public sealed record QuoteState(
    int Version, Guid Id, Guid IssuerId, Guid CustomerId, QuoteStatus Status, IReadOnlyList<QuoteLineState> Lines, IReadOnlyList<LegalMention> Mentions,
    QuoteFinalizationState? Finalization, VatRegime VatRegime = VatRegime.Normal)
{
    public const int CurrentVersion = 3;
}

public sealed record QuoteFinalizationState(
    string Number, DateOnly IssuedOn, DateOnly ValidUntil,
    decimal TotalExcludingVat, IReadOnlyList<VatBreakdownState> VatBreakdown, decimal TotalVat, decimal TotalIncludingVat);

public sealed record VatBreakdownState(decimal Rate, decimal BaseExcludingVat, decimal Vat);

public sealed record QuoteLineState(
    Guid Id, string Designation, decimal Quantity, string Unit, decimal UnitPriceExcludingVat, decimal VatRate, decimal DiscountRate);

public sealed partial class Quote : IPersistable<Quote, QuoteState>
{
    // Rebuilds a stored quote: no event is raised and the creation rules (whitelist) are not replayed.
    public static Quote FromState(QuoteState stored)
    {
        var state = Upcast(stored);
        StateGuard.RequireVersion(state.Version, QuoteState.CurrentVersion);
        StateGuard.Require(state.Id != Guid.Empty && state.IssuerId != Guid.Empty && state.CustomerId != Guid.Empty,
            "the quote, its issuer or its customer has no identifier");
        StateGuard.Require(Enum.IsDefined(state.Status), $"status {state.Status} is unknown");
        StateGuard.Require(state.Lines.Select(line => line.Id).Distinct().Count() == state.Lines.Count, "two lines share an identifier");
        StateGuard.Require(state.Mentions.All(mention => Enum.IsDefined(mention.Code)), "a mention code is unknown");
        StateGuard.Require(state.Mentions.Select(mention => mention.Code).Distinct().Count() == state.Mentions.Count, "two mentions share a code");
        StateGuard.Require(Enum.IsDefined(state.VatRegime), $"VAT regime {state.VatRegime} is unknown");
        StateGuard.Require(state.VatRegime != VatRegime.Franchise || state.Lines.All(line => line.VatRate == 0m),
            "a quote in VAT franchise has a line with a VAT rate");
        StateGuard.Require((state.Status == QuoteStatus.Finalized) == (state.Finalization is not null),
            "a quote is finalized if and only if it has a finalization");

        var quote = new Quote(new QuoteId(state.Id), new IssuerId(state.IssuerId), new CustomerId(state.CustomerId), state.VatRegime);
        quote._lines.AddRange(state.Lines.Select(QuoteLine.FromState));

        foreach (var mention in state.Mentions)
        {
            quote._mentions[mention.Code] = mention;
        }

        if (state.Finalization is { } finalization)
        {
            quote.RestoreFinalization(finalization);
        }

        return quote;
    }

    public QuoteState ToState() => new(
        QuoteState.CurrentVersion,
        Id.Value,
        IssuerId.Value,
        CustomerId.Value,
        Status,
        [.. _lines.Select(line => line.ToState())],
        Mentions,
        Number is { } number
            ? new QuoteFinalizationState(
                number.ToString(), IssuedOn!.Value, ValidUntil!.Value,
                TotalExcludingVat.Amount,
                [.. VatBreakdown.Select(rate => new VatBreakdownState(rate.Rate.Percent, rate.BaseExcludingVat.Amount, rate.Vat.Amount))],
                TotalVat.Amount,
                TotalIncludingVat.Amount)
            : null,
        VatRegime);

    // One converter per version, applied on read and never written back (ADR-0009). Versions 2 and 3 only added
    // fields (the finalization, then the VAT regime), so an older file reads into the current record, and each
    // converter checks what the older version could hold before raising the version.
    private static QuoteState Upcast(QuoteState stored)
    {
        var state = stored;
        if (state.Version == 1)
        {
            StateGuard.Require(state.Status == QuoteStatus.Draft && state.Finalization is null, "a version 1 quote is a draft");
            state = state with { Version = 2 };
        }

        if (state.Version == 2)
        {
            StateGuard.Require(state.VatRegime == VatRegime.Normal, "a version 2 quote is at the normal VAT regime");
            state = state with { Version = 3 };
        }

        return state;
    }

    private void RestoreFinalization(QuoteFinalizationState finalization)
    {
        StateGuard.Require(QuoteNumber.TryParse(finalization.Number, out var number), $"number '{finalization.Number}' is not a quote number");
        StateGuard.Require(number.Year == finalization.IssuedOn.Year, $"number {number} is not of the year {finalization.IssuedOn.Year}");
        StateGuard.Require(finalization.ValidUntil > finalization.IssuedOn, "the validity does not end after the issue date");
        StateGuard.Require(_lines.Count > 0, "a finalized quote has no line");
        StateGuard.Require(_mentions.ContainsKey(MentionCode.QuoteValidity), "a finalized quote has no validity mention");
        StateGuard.Require(
            finalization.TotalExcludingVat == finalization.VatBreakdown.Sum(rate => rate.BaseExcludingVat)
            && finalization.TotalVat == finalization.VatBreakdown.Sum(rate => rate.Vat)
            && finalization.TotalIncludingVat == finalization.TotalExcludingVat + finalization.TotalVat,
            "the issued totals do not add up");

        (Status, Number, IssuedOn, ValidUntil) = (QuoteStatus.Finalized, number, finalization.IssuedOn, finalization.ValidUntil);
        _issued = new IssuedAmounts(
            new Money(finalization.TotalExcludingVat),
            [.. finalization.VatBreakdown.Select(rate => new VatBreakdownLine(new VatRate(rate.Rate), new Money(rate.BaseExcludingVat), new Money(rate.Vat)))]);
    }
}

public sealed partial class QuoteLine
{
    internal static QuoteLine FromState(QuoteLineState line) => new(new QuoteLineId(line.Id), new NewQuoteLine
    {
        Designation = line.Designation,
        Quantity = new Quantity(line.Quantity),
        Unit = line.Unit,
        UnitPriceExcludingVat = new Money(line.UnitPriceExcludingVat),
        VatRate = new VatRate(line.VatRate),
        Discount = new DiscountRate(line.DiscountRate),
    });

    internal QuoteLineState ToState() => new(Id.Value, Designation, Quantity.Value, Unit, UnitPriceExcludingVat.Amount, VatRate.Percent, Discount.Percent);
}
