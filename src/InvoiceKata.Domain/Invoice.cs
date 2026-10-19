namespace InvoiceKata.Domain;

// The texts of the payment mentions an invoice must carry, composed by the application from its text catalog.
public sealed record PaymentMentionTexts(string DueDate, string EarlyPaymentDiscount, string LatePaymentPenalties, string RecoveryIndemnity);

// An invoice is issued from a finalized quote and never changes afterwards (ADR-0009, ADR-0010): it has no method that
// alters it. It copies the quote as issued, its lines, amounts and mentions, and keeps them even if a rule changes later.
public sealed partial class Invoice
{
    private readonly List<QuoteLine> _lines = [];
    private readonly SortedDictionary<MentionCode, LegalMention> _mentions = [];
    private readonly List<IInvoiceEvent> _events = [];

    private Invoice(InvoiceId id, IssuerId issuerId, CustomerId customerId, QuoteId quoteId, QuoteNumber quoteNumber, DateOnly quoteIssuedOn,
        InvoiceNumber number, DateOnly issuedOn, DateOnly dueOn, Money totalExcludingVat, IReadOnlyList<VatBreakdownLine> vatBreakdown)
    {
        Id = id;
        IssuerId = issuerId;
        CustomerId = customerId;
        QuoteId = quoteId;
        QuoteNumber = quoteNumber;
        QuoteIssuedOn = quoteIssuedOn;
        Number = number;
        IssuedOn = issuedOn;
        DueOn = dueOn;
        TotalExcludingVat = totalExcludingVat;
        VatBreakdown = vatBreakdown;
    }

    public InvoiceId Id { get; }

    public IssuerId IssuerId { get; }

    public CustomerId CustomerId { get; }

    // The quote it was issued from, which the invoice refers to ("D'après le devis n° …").
    public QuoteId QuoteId { get; }

    public QuoteNumber QuoteNumber { get; }

    public DateOnly QuoteIssuedOn { get; }

    public InvoiceNumber Number { get; }

    public DateOnly IssuedOn { get; }

    public DateOnly DueOn { get; }

    // The quote's lines, as issued.
    public IReadOnlyList<QuoteLine> Lines => _lines;

    public Money TotalExcludingVat { get; }

    public IReadOnlyList<VatBreakdownLine> VatBreakdown { get; }

    public Money TotalVat => VatBreakdown.Aggregate(Money.Zero, (total, rate) => total + rate.Vat);

    public Money TotalIncludingVat => TotalExcludingVat + TotalVat;

    public IReadOnlyList<LegalMention> Mentions => [.. _mentions.Values];

    /// <exception cref="QuoteNotFinalizedException">The quote is a draft: only a finalized quote is invoiced.</exception>
    public static Invoice IssueFor(Quote quote, InvoiceNumber number, DateOnly issuedOn, DateOnly dueOn, PaymentMentionTexts payment)
    {
        if (quote.Status != QuoteStatus.Finalized)
        {
            throw new QuoteNotFinalizedException(quote.Id);
        }

        if (number.Year != issuedOn.Year)
        {
            throw new ArgumentException($"Number {number} is not of the year {issuedOn.Year}.", nameof(number));
        }

        if (dueOn <= issuedOn)
        {
            throw new ArgumentException("The due date must come after the issue date.", nameof(dueOn));
        }

        var invoice = new Invoice(
            InvoiceId.New(), quote.IssuerId, quote.CustomerId, quote.Id, quote.Number!.Value, quote.IssuedOn!.Value,
            number, issuedOn, dueOn, quote.TotalExcludingVat, quote.VatBreakdown);
        invoice._lines.AddRange(quote.Lines);
        foreach (var mention in quote.Mentions.Where(mention => mention.Code != MentionCode.QuoteValidity))
        {
            invoice._mentions[mention.Code] = mention;
        }

        invoice.Put(MentionCode.PaymentDueDate, payment.DueDate);
        invoice.Put(MentionCode.EarlyPaymentDiscount, payment.EarlyPaymentDiscount);
        invoice.Put(MentionCode.LatePaymentPenalties, payment.LatePaymentPenalties);
        invoice.Put(MentionCode.RecoveryIndemnity, payment.RecoveryIndemnity);
        invoice._events.Add(new InvoiceIssued(invoice.Id, invoice.IssuerId, number));
        return invoice;
    }

    // Hands the recorded events over to the application, once.
    public IReadOnlyList<IInvoiceEvent> TakeEvents()
    {
        IReadOnlyList<IInvoiceEvent> taken = [.. _events];
        _events.Clear();
        return taken;
    }

    private void Put(MentionCode code, string text) => _mentions[code] = new LegalMention(code, text);
}
