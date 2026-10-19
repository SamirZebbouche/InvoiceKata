using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices.Mentions;

// What a payment mention rule reads when the invoice is issued: the finalized quote and the due date chosen then.
public sealed record InvoiceScope(Quote Quote, DateOnly DueOn);

// A payment mention of the invoice (ADR-0012), asked once, at issue: an invoice never changes afterwards.
public interface IInvoiceMentionRule
{
    LegalMention? MentionFor(InvoiceScope scope);
}

// Template Method (CDR-0001), as for the quote: the base fixes the mechanism; a rule says when it applies and what it says.
public abstract class InvoiceMentionRule(IMentionTexts texts) : IInvoiceMentionRule
{
    protected IMentionTexts Texts { get; } = texts;

    protected abstract MentionCode Code { get; }

    public LegalMention? MentionFor(InvoiceScope scope) => AppliesTo(scope) ? new LegalMention(Code, Compose(scope)) : null;

    protected abstract bool AppliesTo(InvoiceScope scope);

    protected abstract string Compose(InvoiceScope scope);
}
