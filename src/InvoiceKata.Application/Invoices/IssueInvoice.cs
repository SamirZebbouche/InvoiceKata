using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Invoices.Mentions;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices;

public sealed class IssueInvoice(
    IQuoteRepository quotes, ICustomerRepository customers, IInvoiceRepository invoices, IEnumerable<IInvoiceMentionRule> mentions,
    TimeProvider clock, InvoiceReactions reactions)
{
    public const int DefaultPaymentDays = 30;

    // Between professionals, the payment term cannot exceed 60 days (ADR-0010).
    public const int MaximumPaymentDays = 60;

    /// <exception cref="QuoteNotFoundException">The quote does not belong to this issuer.</exception>
    /// <exception cref="QuoteNotFinalizedException">The quote is a draft.</exception>
    /// <exception cref="QuoteAlreadyInvoicedException">The quote already has its invoice.</exception>
    public async Task<InvoiceId> HandleAsync(IssuerId issuerId, QuoteId quoteId, int paymentDays, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(paymentDays, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(paymentDays, MaximumPaymentDays);
        var quote = await quotes.GetAsync(issuerId, quoteId, cancellationToken);

        // The use case's half of "one invoice per quote"; the port refuses the duplicate a concurrent issue could create.
        if (await invoices.FindByQuoteAsync(issuerId, quoteId, cancellationToken) is not null)
        {
            throw new QuoteAlreadyInvoicedException(quoteId);
        }

        var issuedOn = ParisCalendar.Today(clock);
        var number = new InvoiceNumbering(await invoices.TakenNumbersAsync(issuerId, cancellationToken)).Next(issuedOn.Year);
        var dueOn = issuedOn.AddDays(paymentDays);
        var scope = new InvoiceScope(quote, await customers.GetAsync(issuerId, quote.CustomerId, cancellationToken), dueOn);
        var invoice = Invoice.IssueFor(quote, number, issuedOn, dueOn, [.. mentions.Select(rule => rule.MentionFor(scope)).OfType<LegalMention>()]);

        await invoices.AddAsync(invoice, cancellationToken);
        await reactions.PublishAsync(invoice.TakeEvents(), cancellationToken);
        return invoice.Id;
    }
}
