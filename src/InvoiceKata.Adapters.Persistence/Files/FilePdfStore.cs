using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.Files;

// issuers/{issuerId}/quotes/{quoteId}.pdf, next to the quote it renders (ADR-0006).
public sealed class FilePdfStore(string directory) : IPdfStore
{
    public async Task SaveIfAbsentAsync(IssuerId issuerId, QuoteId quoteId, byte[] pdf, CancellationToken cancellationToken)
    {
        var path = PathOf(issuerId, quoteId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Atomic and never written over: the move fails if a PDF is already there, and that first one stays.
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temporary, pdf, cancellationToken);
        try
        {
            File.Move(temporary, path, overwrite: false);
        }
        catch (IOException) when (File.Exists(path))
        {
            File.Delete(temporary);
        }
    }

    public async Task<byte[]?> FindAsync(IssuerId issuerId, QuoteId quoteId, CancellationToken cancellationToken)
    {
        var path = PathOf(issuerId, quoteId);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    private string PathOf(IssuerId issuerId, QuoteId quoteId) =>
        Path.Combine(directory, IssuerFolder.Of(issuerId), "quotes", $"{quoteId.Value}.pdf");
}
