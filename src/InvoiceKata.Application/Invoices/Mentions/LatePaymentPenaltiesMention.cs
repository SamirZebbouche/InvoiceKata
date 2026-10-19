using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices.Mentions;

// The rate of the late payment penalties.
public sealed class LatePaymentPenaltiesMention(IMentionTexts texts) : InvoiceMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.LatePaymentPenalties;

    protected override bool AppliesTo(InvoiceScope scope) => true;

    protected override string Compose(InvoiceScope scope) => Texts.LatePaymentPenalties();
}
