using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices.Mentions;

// The 40 € flat indemnity for recovery costs.
public sealed class RecoveryIndemnityMention(IMentionTexts texts) : InvoiceMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.RecoveryIndemnity;

    protected override bool AppliesTo(InvoiceScope scope) => true;

    protected override string Compose(InvoiceScope scope) => Texts.RecoveryIndemnity();
}
