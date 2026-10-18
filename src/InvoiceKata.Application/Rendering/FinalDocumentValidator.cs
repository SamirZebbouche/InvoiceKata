namespace InvoiceKata.Application.Rendering;

// One part of the contract between the domain and the rendering: it lists what is wrong in its part, and never throws.
public interface IDocumentPartValidator
{
    IEnumerable<string> ViolationsIn(ResolvedDocument document);
}

// Composite (ADR-0005, decision 9): the final validator is made of the validators of each part, and every part speaks,
// so a single refusal lists everything that is wrong. No PDF is rendered from a refused document.
public sealed class FinalDocumentValidator(IEnumerable<IDocumentPartValidator> parts)
{
    /// <exception cref="InvalidDocumentException">A part of the document breaks the contract.</exception>
    public void Validate(ResolvedDocument document)
    {
        var violations = parts.SelectMany(part => part.ViolationsIn(document)).ToList();
        if (violations.Count > 0)
        {
            throw new InvalidDocumentException(violations);
        }
    }
}

// A defect of the product, not of the request: the domain resolved an incomplete document. The API answers 500.
public sealed class InvalidDocumentException(IReadOnlyList<string> violations)
    : Exception($"The document cannot be rendered: {string.Join("; ", violations)}.")
{
    public IReadOnlyList<string> Violations { get; } = violations;
}
