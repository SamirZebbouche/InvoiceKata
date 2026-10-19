using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class FinalizeQuote(IQuoteRepository quotes, IMentionTexts texts, TimeProvider clock, QuoteReactions reactions)
{
    public const int DefaultValidityDays = 30;

    // The validity mention is composed here, not by a rule: the quote puts it and freezes in one step
    // (CDR-0001 amendment). QuoteFinalized is published after the save, to reactions such as the PDF rendering.
    /// <exception cref="QuoteNotFoundException">The quote does not belong to this issuer.</exception>
    /// <exception cref="QuoteAlreadyFinalizedException">The quote is already finalized.</exception>
    /// <exception cref="QuoteWithoutLinesException">The quote has no line.</exception>
    public async Task HandleAsync(IssuerId issuerId, QuoteId quoteId, int validityDays, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(validityDays, 1);
        var quote = await quotes.GetAsync(issuerId, quoteId, cancellationToken);

        var issuedOn = ParisCalendar.Today(clock);
        var number = new QuoteNumbering(await quotes.TakenNumbersAsync(issuerId, cancellationToken)).Next(issuedOn.Year);
        var validUntil = issuedOn.AddDays(validityDays);
        quote.Finalize(number, issuedOn, validUntil, texts.QuoteValidity(validityDays, validUntil));

        await quotes.SaveAsync(quote, cancellationToken);
        await reactions.PublishAsync(quote.TakeEvents(), cancellationToken);
    }
}
