namespace InvoiceKata.Application.Rendering;

// Secondary port (ADR-0005): a resolved document in, a PDF out. The adapter only lays the document out.
public interface IDocumentRenderer
{
    Task<byte[]> RenderAsync(ResolvedDocument document, CancellationToken cancellationToken);
}
