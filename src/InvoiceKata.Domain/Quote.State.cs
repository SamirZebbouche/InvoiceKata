namespace InvoiceKata.Domain;

// What a quote stores: its inputs and its mentions. A draft recomputes its totals and VAT breakdown; a
// finalized quote stores them as issued, with its number and dates (CDR-0006, version 2).
public sealed record QuoteState(
    int Version, Guid Id, Guid IssuerId, Guid CustomerId, QuoteStatus Status, IReadOnlyList<QuoteLineState> Lines, IReadOnlyList<LegalMention> Mentions,
    QuoteFinalizationState? Finalization)
{
    public const int CurrentVersion = 2;
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
        StateGuard.Require((state.Status == QuoteStatus.Finalized) == (state.Finalization is not null),
            "a quote is finalized if and only if it has a finalization");

        var quote = new Quote(new QuoteId(state.Id), new IssuerId(state.IssuerId), new CustomerId(state.CustomerId));
        foreach (var line in state.Lines)
        {
            quote._lines.Add(new QuoteLine(new QuoteLineId(line.Id), new NewQuoteLine
            {
                Designation = line.Designation,
                Quantity = new Quantity(line.Quantity),
                Unit = line.Unit,
                UnitPriceExcludingVat = new Money(line.UnitPriceExcludingVat),
                VatRate = new VatRate(line.VatRate),
                Discount = new DiscountRate(line.DiscountRate),
            }));
        }

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
        [.. _lines.Select(line => new QuoteLineState(
            line.Id.Value, line.Designation, line.Quantity.Value, line.Unit,
            line.UnitPriceExcludingVat.Amount, line.VatRate.Percent, line.Discount.Percent))],
        Mentions,
        Number is { } number
            ? new QuoteFinalizationState(
                number.ToString(), IssuedOn!.Value, ValidUntil!.Value,
                TotalExcludingVat.Amount,
                [.. VatBreakdown.Select(rate => new VatBreakdownState(rate.Rate.Percent, rate.BaseExcludingVat.Amount, rate.Vat.Amount))],
                TotalVat.Amount,
                TotalIncludingVat.Amount)
            : null);

    // One converter per version, applied on read and never written back (ADR-0009). Version 2 only added the
    // finalization, so a version 1 file reads into the current record, and its converter checks it was a draft.
    private static QuoteState Upcast(QuoteState stored)
    {
        if (stored.Version != 1)
        {
            return stored;
        }

        StateGuard.Require(stored.Status == QuoteStatus.Draft && stored.Finalization is null, "a version 1 quote is a draft");
        return stored with { Version = 2 };
    }

    private void RestoreFinalization(QuoteFinalizationState finalization)
    {
        StateGuard.Require(QuoteNumber.TryParse(finalization.Number, out var number), $"number '{finalization.Number}' is not a quote number");
        StateGuard.Require(number.Year == finalization.IssuedOn.Year, $"number {number} is not of the year {finalization.IssuedOn.Year}");
        StateGuard.Require(finalization.ValidUntil > finalization.IssuedOn, "the validity does not end after the issue date");
        StateGuard.Require(_lines.Count > 0, "a finalized quote has no line");
        StateGuard.Require(_mentions.ContainsKey(MentionCode.QuoteValidity), "a finalized quote has no validity mention");

        (Status, Number, IssuedOn, ValidUntil) = (QuoteStatus.Finalized, number, finalization.IssuedOn, finalization.ValidUntil);
        _issued = new IssuedAmounts(
            new Money(finalization.TotalExcludingVat),
            [.. finalization.VatBreakdown.Select(rate => new VatBreakdownLine(new VatRate(rate.Rate), new Money(rate.BaseExcludingVat), new Money(rate.Vat)))],
            new Money(finalization.TotalVat),
            new Money(finalization.TotalIncludingVat));
    }
}
