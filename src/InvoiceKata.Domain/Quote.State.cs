namespace InvoiceKata.Domain;

// What a draft stores: its inputs and its mentions. Totals and the VAT breakdown are recomputed (CDR-0006).
public sealed record QuoteState(
    int Version, Guid Id, Guid IssuerId, Guid CustomerId, QuoteStatus Status, IReadOnlyList<QuoteLineState> Lines, IReadOnlyList<LegalMention> Mentions)
{
    public const int CurrentVersion = 1;
}

public sealed record QuoteLineState(
    Guid Id, string Designation, decimal Quantity, string Unit, decimal UnitPriceExcludingVat, decimal VatRate, decimal DiscountRate);

public sealed partial class Quote : IPersistable<Quote, QuoteState>
{
    // Rebuilds a stored quote: no event is raised and the creation rules (whitelist) are not replayed.
    public static Quote FromState(QuoteState state)
    {
        StateGuard.RequireVersion(state.Version, QuoteState.CurrentVersion);
        StateGuard.Require(state.Id != Guid.Empty && state.IssuerId != Guid.Empty && state.CustomerId != Guid.Empty,
            "the quote, its issuer or its customer has no identifier");
        StateGuard.Require(Enum.IsDefined(state.Status), $"status {state.Status} is unknown");
        StateGuard.Require(state.Lines.Select(line => line.Id).Distinct().Count() == state.Lines.Count, "two lines share an identifier");
        StateGuard.Require(state.Mentions.All(mention => Enum.IsDefined(mention.Code)), "a mention code is unknown");
        StateGuard.Require(state.Mentions.Select(mention => mention.Code).Distinct().Count() == state.Mentions.Count, "two mentions share a code");

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
        Mentions);
}
