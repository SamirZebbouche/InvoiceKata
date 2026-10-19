using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

// A context mention: it only depends on the issuer, so it is there from the draft on. Every kind of issuer has one;
// its text depends on the kind (ADR-0011), and one rule only puts it, since two rules under one code would undo each other.
public sealed class IssuerLegalIdentityMention(IMentionTexts texts) : LegalMentionRule(texts)
{
    protected override MentionCode Code => MentionCode.IssuerLegalIdentity;

    protected override bool AppliesTo(QuoteScope scope) => true;

    protected override string Compose(QuoteScope scope) => Texts.IssuerLegalIdentity(scope.Issuer);
}
