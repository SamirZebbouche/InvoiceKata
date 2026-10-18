using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

// A mention triggered by lines: present while at least one line is at the reduced rate of 10 %.
public sealed class ReducedVatRateMention(IMentionTexts texts) : LegalMentionRule(texts)
{
    private static readonly VatRate ReducedRate = new(10m);

    protected override MentionCode Code => MentionCode.ReducedVatRate;

    protected override bool AppliesTo(QuoteScope scope) => scope.Quote.Lines.Any(line => line.VatRate == ReducedRate);

    protected override string Compose(QuoteScope scope) => Texts.ReducedVatRate();
}
