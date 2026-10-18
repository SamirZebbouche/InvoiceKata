using InvoiceKata.Domain;

using Microsoft.Extensions.Logging;

namespace InvoiceKata.Application.Quotes;

// A reaction to a fact already recorded, such as rendering the PDF of a finalized quote (ADR-0003).
public interface IQuoteReaction
{
    Task ReactAsync(IQuoteEvent quoteEvent, CancellationToken cancellationToken);
}

// The second dispatch point: after the save, never before, so no reaction starts from an unrecorded fact.
public sealed class QuoteReactions(IEnumerable<IQuoteReaction> reactions, ILogger<QuoteReactions> logger)
{
    public async Task PublishAsync(IReadOnlyList<IQuoteEvent> events, CancellationToken cancellationToken)
    {
        foreach (var quoteEvent in events)
        {
            foreach (var reaction in reactions)
            {
                try
                {
                    await reaction.ReactAsync(quoteEvent, cancellationToken);
                }
                catch (Exception failure) when (failure is not OperationCanceledException)
                {
                    // The fact stays recorded and the other reactions still run; an idempotent reaction is retried later.
                    logger.LogError(failure, "{Reaction} failed to react to {Event}.", reaction.GetType().Name, quoteEvent);
                }
            }
        }
    }
}
