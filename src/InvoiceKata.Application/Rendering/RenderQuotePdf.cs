using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

// Renders the PDF of a finalized quote, once (ADR-0005 amendment): as a reaction to QuoteFinalized, and again when
// the PDF is read but missing, for instance because the reaction failed. A stored PDF is never rendered again.
public sealed class RenderQuotePdf(IQuoteRepository quotes, QuoteScopes scopes, FinalDocumentValidator validator, IDocumentRenderer renderer, IPdfStore pdfs)
    : IQuoteReaction
{
    public async Task ReactAsync(IQuoteEvent quoteEvent, CancellationToken cancellationToken)
    {
        if (quoteEvent is QuoteFinalized finalized)
        {
            await PdfOfAsync(finalized.IssuerId, finalized.QuoteId, cancellationToken);
        }
    }

    /// <exception cref="QuoteNotFoundException">The quote does not belong to this issuer.</exception>
    /// <exception cref="QuoteNotFinalizedException">The quote is a draft.</exception>
    /// <exception cref="InvalidDocumentException">The final validator refuses the document: no PDF is rendered.</exception>
    public async Task<byte[]> PdfOfAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken)
    {
        if (await pdfs.FindAsync(issuerId, quoteId, cancellationToken) is { } stored)
        {
            return stored;
        }

        var quote = await quotes.GetAsync(issuerId, quoteId, cancellationToken);
        if (quote.Status != QuoteStatus.Finalized)
        {
            throw new QuoteNotFinalizedException(quote.Id);
        }

        var document = ResolvedDocument.OfQuote(await scopes.ForAsync(quote, cancellationToken));
        validator.Validate(document);
        await pdfs.SaveIfAbsentAsync(issuerId, quoteId, await renderer.RenderAsync(document, cancellationToken), cancellationToken);
        return (await pdfs.FindAsync(issuerId, quoteId, cancellationToken))!;
    }
}
