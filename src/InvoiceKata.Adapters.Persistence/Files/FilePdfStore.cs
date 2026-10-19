using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.Files;

// issuers/{issuerId}/quotes/{quoteId}.pdf or invoices/{invoiceId}.pdf, next to the document it renders (ADR-0006).
public sealed class FilePdfStore(string directory) : IPdfStore
{
    public async Task SaveIfAbsentAsync(IssuerId issuerId, DocumentReference document, byte[] pdf, CancellationToken cancellationToken)
    {
        var path = PathOf(issuerId, document);
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

    public async Task<byte[]?> FindAsync(IssuerId issuerId, DocumentReference document, CancellationToken cancellationToken)
    {
        var path = PathOf(issuerId, document);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    private string PathOf(IssuerId issuerId, DocumentReference document) =>
        Path.Combine(directory, IssuerFolder.Of(issuerId), FolderOf(document.Kind), $"{document.Id}.pdf");

    private static string FolderOf(DocumentKind kind) => kind switch
    {
        DocumentKind.Quote => "quotes",
        DocumentKind.Invoice => "invoices",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "This kind of document has no folder."),
    };
}
