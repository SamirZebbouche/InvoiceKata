using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

// Template Method (CDR-0001): the base fixes the mechanism once; a derived rule only says when it applies and what it says.
// Whatever the event, the rule re-evaluates the current state of the quote, so replaying an event changes nothing.
public abstract class LegalMentionRule(IMentionTexts texts) : IQuoteEventHandler
{
    protected IMentionTexts Texts { get; } = texts;

    protected abstract MentionCode Code { get; }

    public void Handle(IQuoteEvent quoteEvent, QuoteScope scope)
    {
        if (AppliesTo(scope))
        {
            scope.Quote.PutMention(new LegalMention(Code, Compose(scope)));
        }
        else
        {
            scope.Quote.RemoveMention(Code);
        }
    }

    protected abstract bool AppliesTo(QuoteScope scope);

    protected abstract string Compose(QuoteScope scope);
}
