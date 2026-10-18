using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

// A context mention: it only depends on the issuer, so it is there from the draft on.
public sealed class IssuerLegalIdentityMention(IMentionTexts texts) : LegalMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.IssuerLegalIdentity;

    protected override bool AppliesTo(QuoteScope scope) => scope.Issuer.Kind == IssuerKind.Company;

    protected override string Compose(QuoteScope scope) => Texts.IssuerLegalIdentity(scope.Issuer);
}
