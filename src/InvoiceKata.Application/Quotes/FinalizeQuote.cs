using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes;

public sealed class FinalizeQuote(IQuoteRepository quotes, IMentionTexts texts, TimeProvider clock)
{
    public const int DefaultValidityDays = 30;

    private static readonly TimeZoneInfo France = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    // The validity mention is composed here, not by a rule: the quote puts it and freezes in one step
    // (CDR-0001 amendment). QuoteFinalized is meant for reactions after the save, such as the PDF rendering.
    /// <exception cref="QuoteNotFoundException">The quote does not belong to this issuer.</exception>
    /// <exception cref="QuoteAlreadyFinalizedException">The quote is already finalized.</exception>
    /// <exception cref="QuoteWithoutLinesException">The quote has no line.</exception>
    public async Task HandleAsync(IssuerId issuerId, QuoteId quoteId, int validityDays, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(validityDays, 1);
        var quote = await quotes.GetAsync(issuerId, quoteId, cancellationToken);

        var issuedOn = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), France).DateTime);
        var number = new QuoteNumbering(await quotes.TakenNumbersAsync(issuerId, cancellationToken)).Next(issuedOn.Year);
        var validUntil = issuedOn.AddDays(validityDays);
        quote.Finalize(number, issuedOn, validUntil, texts.QuoteValidity(validityDays, validUntil));

        await quotes.SaveAsync(quote, cancellationToken);
    }
}
