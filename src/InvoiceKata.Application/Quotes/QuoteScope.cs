using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

// What a quote event handler can read: the quote, and the issuer and customer it was drafted for.
public sealed record QuoteScope(Quote Quote, Issuer Issuer, Customer Customer)
{
    public BillingContext Context => new(Issuer.Kind, Customer.Kind);
}

public interface IQuoteEventHandler
{
    void Handle(IQuoteEvent quoteEvent, QuoteScope scope);
}

// In-memory, in-process (CDR-0002). Runs inside the use case, before the quote is saved (ADR-0003, amendment).
public sealed class QuoteEventDispatcher(IEnumerable<IQuoteEventHandler> handlers)
{
    public void DispatchPendingEvents(QuoteScope scope)
    {
        foreach (var quoteEvent in scope.Quote.TakeEvents())
        {
            foreach (var handler in handlers)
            {
                handler.Handle(quoteEvent, scope);
            }
        }
    }
}
