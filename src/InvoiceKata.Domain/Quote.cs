namespace InvoiceKata.Domain;

public enum QuoteStatus
{
    Draft,
}

// A quote is drafted for a customer and belongs to that customer's issuer: it cannot cross issuers by construction.
public sealed class Quote
{
    private readonly List<QuoteLine> _lines = [];

    private Quote(QuoteId id, IssuerId issuerId, CustomerId customerId)
    {
        Id = id;
        IssuerId = issuerId;
        CustomerId = customerId;
    }

    public QuoteId Id { get; }

    public IssuerId IssuerId { get; }

    public CustomerId CustomerId { get; }

    public QuoteStatus Status { get; } = QuoteStatus.Draft;

    public IReadOnlyList<QuoteLine> Lines => _lines;

    public Money TotalExcludingVat => _lines.Aggregate(Money.Zero, (total, line) => total + line.TotalExcludingVat);

    // VAT is computed once per rate, on the total excluding VAT of that rate, never line by line.
    public IReadOnlyList<VatBreakdownLine> VatBreakdown =>
        [.. _lines
            .GroupBy(line => line.VatRate)
            .OrderBy(group => group.Key.Percent)
            .Select(group =>
            {
                var baseExcludingVat = group.Aggregate(Money.Zero, (total, line) => total + line.TotalExcludingVat);
                return new VatBreakdownLine(group.Key, baseExcludingVat, group.Key.VatOn(baseExcludingVat));
            })];

    public Money TotalVat => VatBreakdown.Aggregate(Money.Zero, (total, rate) => total + rate.Vat);

    public Money TotalIncludingVat => TotalExcludingVat + TotalVat;

    /// <exception cref="UnsupportedBillingContextException">The product does not handle this issuer and customer pair.</exception>
    public static Quote DraftFor(Issuer issuer, Customer customer)
    {
        if (customer.IssuerId != issuer.Id)
        {
            throw new ArgumentException("The customer belongs to another issuer.", nameof(customer));
        }

        var context = new BillingContext(issuer.Kind, customer.Kind);
        if (!context.IsSupported)
        {
            throw new UnsupportedBillingContextException(context);
        }

        return new Quote(QuoteId.New(), issuer.Id, customer.Id);
    }

    public QuoteLineId AddLine(NewQuoteLine line)
    {
        var added = new QuoteLine(QuoteLineId.New(), line);
        _lines.Add(added);
        return added.Id;
    }

    /// <exception cref="QuoteLineNotFoundException">The line is not on this quote.</exception>
    public void RemoveLine(QuoteLineId lineId)
    {
        if (_lines.RemoveAll(line => line.Id == lineId) == 0)
        {
            throw new QuoteLineNotFoundException(Id, lineId);
        }
    }
}
