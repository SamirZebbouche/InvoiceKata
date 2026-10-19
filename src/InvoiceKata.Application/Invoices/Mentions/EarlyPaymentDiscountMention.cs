using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Invoices.Mentions;

// No discount for early payment, which the invoice must still say.
public sealed class EarlyPaymentDiscountMention(IMentionTexts texts) : InvoiceMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.EarlyPaymentDiscount;

    protected override bool AppliesTo(InvoiceScope scope) => true;

    protected override string Compose(InvoiceScope scope) => Texts.NoEarlyPaymentDiscount();
}
