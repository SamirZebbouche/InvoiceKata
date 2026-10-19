using System.Globalization;
using System.Text.RegularExpressions;

namespace InvoiceKata.Domain;

// "F-2026-0001": the year of the issue, then a sequence per issuer, restarted every year, with no gap (ADR-0010).
public readonly partial record struct InvoiceNumber
{
    public InvoiceNumber(int year, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1000);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);
        Year = year;
        Sequence = sequence;
    }

    public int Year { get; }

    public int Sequence { get; }

    /// <exception cref="FormatException">The text is not an invoice number.</exception>
    public static InvoiceNumber Parse(string text) =>
        TryParse(text, out var number) ? number : throw new FormatException($"'{text}' is not an invoice number.");

    public static bool TryParse(string text, out InvoiceNumber number)
    {
        var match = Pattern().Match(text);
        var sequence = match.Success ? int.Parse(match.Groups["sequence"].Value, CultureInfo.InvariantCulture) : 0;
        number = sequence > 0 ? new InvoiceNumber(int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture), sequence) : default;
        return sequence > 0;
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"F-{Year}-{Sequence:D4}");

    [GeneratedRegex(@"^F-(?<year>\d{4})-(?<sequence>\d{4,9})$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
