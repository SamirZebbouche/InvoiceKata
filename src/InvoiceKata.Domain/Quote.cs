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

    public static Quote DraftFor(Customer customer) => new(QuoteId.New(), customer.IssuerId, customer.Id);
}
