using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices.Mentions;

// The 40 € flat indemnity for recovery costs, which only a professional customer can be asked for (ADR-0012).
public sealed class RecoveryIndemnityMention(IMentionTexts texts) : InvoiceMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.RecoveryIndemnity;

    protected override bool AppliesTo(InvoiceScope scope) => scope.Customer.Kind == CustomerKind.Professional;

    protected override string Compose(InvoiceScope scope) => Texts.RecoveryIndemnity();
}
