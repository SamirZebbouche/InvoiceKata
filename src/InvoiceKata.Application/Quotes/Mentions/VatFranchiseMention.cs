using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

// A context mention: an issuer in VAT franchise says so on every document (ADR-0011).
public sealed class VatFranchiseMention(IMentionTexts texts) : LegalMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.VatFranchise;

    protected override bool AppliesTo(QuoteScope scope) => scope.Quote.VatRegime == VatRegime.Franchise;

    protected override string Compose(QuoteScope scope) => Texts.VatFranchise();
}
