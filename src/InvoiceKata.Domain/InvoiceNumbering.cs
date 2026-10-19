namespace InvoiceKata.Domain;

// The domain's half of the guarantee against duplicate numbers (ADR-0010, as CDR-0007): given the numbers an issuer has
// already taken, the next one follows the greatest of the year, so it can never be one of them. The port
// refuses the duplicate a concurrent issue could still produce.
public sealed class InvoiceNumbering(IEnumerable<InvoiceNumber> taken)
{
    public InvoiceNumber Next(int year) =>
        new(year, taken.Where(number => number.Year == year).Select(number => number.Sequence).DefaultIfEmpty(0).Max() + 1);
}
