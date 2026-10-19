using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices.Mentions;

// The due date chosen at issue.
public sealed class PaymentDueDateMention(IMentionTexts texts) : InvoiceMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.PaymentDueDate;

    protected override bool AppliesTo(InvoiceScope scope) => true;

    protected override string Compose(InvoiceScope scope) => Texts.InvoiceDueDate(scope.DueOn);
}
