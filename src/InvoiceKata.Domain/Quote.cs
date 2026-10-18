namespace InvoiceKata.Domain;

public enum QuoteStatus
{
    Draft,
    Finalized,
}

// A quote is drafted for a customer and belongs to that customer's issuer: it cannot cross issuers by construction.
public sealed partial class Quote
{
    private readonly List<QuoteLine> _lines = [];
    private readonly SortedDictionary<MentionCode, LegalMention> _mentions = [];
    private readonly List<IQuoteEvent> _events = [];

    private Quote(QuoteId id, IssuerId issuerId, CustomerId customerId)
    {
        Id = id;
        IssuerId = issuerId;
        CustomerId = customerId;
    }

    public QuoteId Id { get; }

    public IssuerId IssuerId { get; }

    public CustomerId CustomerId { get; }

    public QuoteStatus Status { get; private set; } = QuoteStatus.Draft;

    // Given at the finalization, never at the creation.
    public QuoteNumber? Number { get; private set; }

    public DateOnly? IssuedOn { get; private set; }

    public DateOnly? ValidUntil { get; private set; }

    public IReadOnlyList<QuoteLine> Lines => _lines;

    // One mention per code: putting the same mention again replaces it, so mention handlers stay idempotent.
    public IReadOnlyList<LegalMention> Mentions => [.. _mentions.Values];

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

        var quote = new Quote(QuoteId.New(), issuer.Id, customer.Id);
        quote._events.Add(new QuoteDrafted(quote.Id));
        return quote;
    }

    /// <exception cref="QuoteAlreadyFinalizedException">The quote is finalized.</exception>
    public QuoteLineId AddLine(NewQuoteLine line)
    {
        EnsureDraft();
        var added = new QuoteLine(QuoteLineId.New(), line);
        _lines.Add(added);
        _events.Add(new QuoteLineAdded(Id, added.Id));
        return added.Id;
    }

    /// <exception cref="QuoteLineNotFoundException">The line is not on this quote.</exception>
    /// <exception cref="QuoteAlreadyFinalizedException">The quote is finalized.</exception>
    public void RemoveLine(QuoteLineId lineId)
    {
        EnsureDraft();
        if (_lines.RemoveAll(line => line.Id == lineId) == 0)
        {
            throw new QuoteLineNotFoundException(Id, lineId);
        }

        _events.Add(new QuoteLineRemoved(Id, lineId));
    }

    // Puts the validity mention and freezes the quote in one step: once finalized, nothing changes it,
    // its mentions included (ADR-0009, CDR-0001 amendment).
    /// <exception cref="QuoteAlreadyFinalizedException">The quote is already finalized.</exception>
    /// <exception cref="QuoteWithoutLinesException">The quote has no line.</exception>
    public void Finalize(QuoteNumber number, DateOnly issuedOn, DateOnly validUntil, string validity)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new QuoteWithoutLinesException(Id);
        }

        if (number.Year != issuedOn.Year)
        {
            throw new ArgumentException($"Number {number} is not of the year {issuedOn.Year}.", nameof(number));
        }

        if (validUntil <= issuedOn)
        {
            throw new ArgumentException("The validity must end after the issue date.", nameof(validUntil));
        }

        _mentions[MentionCode.QuoteValidity] = new LegalMention(MentionCode.QuoteValidity, validity);
        (Status, Number, IssuedOn, ValidUntil) = (QuoteStatus.Finalized, number, issuedOn, validUntil);
        _events.Add(new QuoteFinalized(Id, number));
    }

    /// <exception cref="QuoteAlreadyFinalizedException">The quote is finalized.</exception>
    public void PutMention(LegalMention mention)
    {
        EnsureDraft();
        _mentions[mention.Code] = mention;
    }

    /// <exception cref="QuoteAlreadyFinalizedException">The quote is finalized.</exception>
    public void RemoveMention(MentionCode code)
    {
        EnsureDraft();
        _mentions.Remove(code);
    }

    private void EnsureDraft()
    {
        if (Status != QuoteStatus.Draft)
        {
            throw new QuoteAlreadyFinalizedException(Id);
        }
    }

    // Hands the recorded events over to the application, once.
    public IReadOnlyList<IQuoteEvent> TakeEvents()
    {
        IReadOnlyList<IQuoteEvent> taken = [.. _events];
        _events.Clear();
        return taken;
    }
}
