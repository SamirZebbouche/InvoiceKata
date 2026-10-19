using InvoiceKata.Domain;

using Microsoft.Extensions.Logging;

namespace InvoiceKata.Application.Invoices;

// A reaction to a fact already recorded, such as rendering the PDF of an issued invoice (ADR-0003).
public interface IInvoiceReaction
{
    Task ReactAsync(IInvoiceEvent invoiceEvent, CancellationToken cancellationToken);
}

// After the save, never before (ADR-0003); a twin of QuoteReactions, to merge with a third kind of document.
public sealed class InvoiceReactions(IEnumerable<IInvoiceReaction> reactions, ILogger<InvoiceReactions> logger)
{
    public async Task PublishAsync(IReadOnlyList<IInvoiceEvent> events, CancellationToken cancellationToken)
    {
        foreach (var invoiceEvent in events)
        {
            foreach (var reaction in reactions)
            {
                try
                {
                    await reaction.ReactAsync(invoiceEvent, cancellationToken);
                }
                catch (Exception failure) when (failure is not OperationCanceledException)
                {
                    // The fact stays recorded and the other reactions still run; an idempotent reaction is retried later.
                    logger.LogError(failure, "{Reaction} failed to react to {Event}.", reaction.GetType().Name, invoiceEvent);
                }
            }
        }
    }
}
