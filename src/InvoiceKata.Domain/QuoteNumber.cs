using System.Globalization;
using System.Text.RegularExpressions;

namespace InvoiceKata.Domain;

// "D-2026-0001": the year of the finalization, then a sequence per issuer, restarted every year (CDR-0007).
public readonly partial record struct QuoteNumber
{
    public QuoteNumber(int year, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1000);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);
        Year = year;
        Sequence = sequence;
    }

    public int Year { get; }

    public int Sequence { get; }

    /// <exception cref="FormatException">The text is not a quote number.</exception>
    public static QuoteNumber Parse(string text)
    {
        var match = Pattern().Match(text);
        if (!match.Success || int.Parse(match.Groups["sequence"].Value, CultureInfo.InvariantCulture) == 0)
        {
            throw new FormatException($"'{text}' is not a quote number.");
        }

        return new QuoteNumber(
            int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["sequence"].Value, CultureInfo.InvariantCulture));
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"D-{Year}-{Sequence:D4}");

    [GeneratedRegex(@"^D-(?<year>\d{4})-(?<sequence>\d{4,9})$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
