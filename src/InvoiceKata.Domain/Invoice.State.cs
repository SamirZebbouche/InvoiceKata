namespace InvoiceKata.Domain;

// What an invoice stores: everything as issued, its amounts included, never recomputed (ADR-0009, ADR-0010).
public sealed record InvoiceState(
    int Version, Guid Id, Guid IssuerId, Guid CustomerId, Guid QuoteId, string QuoteNumber, DateOnly QuoteIssuedOn,
    string Number, DateOnly IssuedOn, DateOnly DueOn, IReadOnlyList<QuoteLineState> Lines,
    decimal TotalExcludingVat, IReadOnlyList<VatBreakdownState> VatBreakdown, decimal TotalVat, decimal TotalIncludingVat,
    IReadOnlyList<LegalMention> Mentions)
{
    public const int CurrentVersion = 1;
}

public sealed partial class Invoice : IPersistable<Invoice, InvoiceState>
{
    private static readonly MentionCode[] PaymentMentions =
        [MentionCode.PaymentDueDate, MentionCode.EarlyPaymentDiscount, MentionCode.LatePaymentPenalties, MentionCode.RecoveryIndemnity];

    // Rebuilds a stored invoice: no event is raised, and its structural invariants are checked again.
    public static Invoice FromState(InvoiceState state)
    {
        StateGuard.RequireVersion(state.Version, InvoiceState.CurrentVersion);
        StateGuard.Require(
            state.Id != Guid.Empty && state.IssuerId != Guid.Empty && state.CustomerId != Guid.Empty && state.QuoteId != Guid.Empty,
            "the invoice, its issuer, its customer or its quote has no identifier");
        StateGuard.Require(InvoiceNumber.TryParse(state.Number, out var number), $"number '{state.Number}' is not an invoice number");
        StateGuard.Require(number.Year == state.IssuedOn.Year, $"number {number} is not of the year {state.IssuedOn.Year}");
        StateGuard.Require(Domain.QuoteNumber.TryParse(state.QuoteNumber, out var quoteNumber), $"quote number '{state.QuoteNumber}' is not a quote number");
        StateGuard.Require(state.DueOn > state.IssuedOn, "the due date does not come after the issue date");
        StateGuard.Require(state.Lines.Count > 0, "an invoice has no line");
        StateGuard.Require(state.Lines.Select(line => line.Id).Distinct().Count() == state.Lines.Count, "two lines share an identifier");
        StateGuard.RequireTotalsAddUp(state.TotalExcludingVat, state.VatBreakdown, state.TotalVat, state.TotalIncludingVat);
        StateGuard.Require(state.Mentions.All(mention => Enum.IsDefined(mention.Code)), "a mention code is unknown");
        StateGuard.Require(state.Mentions.Select(mention => mention.Code).Distinct().Count() == state.Mentions.Count, "two mentions share a code");
        StateGuard.Require(PaymentMentions.All(code => state.Mentions.Any(mention => mention.Code == code)), "an invoice lacks a payment mention");

        var invoice = new Invoice(
            new InvoiceId(state.Id), new IssuerId(state.IssuerId), new CustomerId(state.CustomerId),
            new QuoteId(state.QuoteId), quoteNumber, state.QuoteIssuedOn, number, state.IssuedOn, state.DueOn,
            new Money(state.TotalExcludingVat),
            [.. state.VatBreakdown.Select(rate => new VatBreakdownLine(new VatRate(rate.Rate), new Money(rate.BaseExcludingVat), new Money(rate.Vat)))]);
        invoice._lines.AddRange(state.Lines.Select(QuoteLine.FromState));
        foreach (var mention in state.Mentions)
        {
            invoice._mentions[mention.Code] = mention;
        }

        return invoice;
    }

    public InvoiceState ToState() => new(
        InvoiceState.CurrentVersion,
        Id.Value,
        IssuerId.Value,
        CustomerId.Value,
        QuoteId.Value,
        QuoteNumber.ToString(),
        QuoteIssuedOn,
        Number.ToString(),
        IssuedOn,
        DueOn,
        [.. _lines.Select(line => line.ToState())],
        TotalExcludingVat.Amount,
        [.. VatBreakdown.Select(rate => new VatBreakdownState(rate.Rate.Percent, rate.BaseExcludingVat.Amount, rate.Vat.Amount))],
        TotalVat.Amount,
        TotalIncludingVat.Amount,
        Mentions);
}
