using System.Globalization;

using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

// The validators of each part of a resolved document (ADR-0005, decision 9). They check the contract
// independently of the code that resolved the document: a defect upstream cannot reach a PDF.
public sealed class HeaderValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document)
    {
        var header = document.Document;
        if (header.Kind != "quote")
        {
            yield return $"the document kind '{header.Kind}' is unknown";
            yield break;
        }

        if (header.Number.Length == 0)
        {
            yield return "the document has no number";
        }
        else if (!QuoteNumber.TryParse(header.Number, out _))
        {
            yield return $"number '{header.Number}' is not a quote number";
        }

        if (!Dates.TryRead(header.IssuedOn, out var issuedOn))
        {
            yield return $"the issue date '{header.IssuedOn}' is not a date";
        }
        else if (header.ValidUntil is null)
        {
            yield return "a quote has no validity date";
        }
        else if (!Dates.TryRead(header.ValidUntil, out var validUntil) || validUntil <= issuedOn)
        {
            yield return "the validity does not end after the issue date";
        }
    }
}

public sealed class IssuerValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document)
    {
        var issuer = document.Issuer;
        if (string.IsNullOrWhiteSpace(issuer.Name))
        {
            yield return "the issuer has no name";
        }

        if (issuer.Siren.Length != 9 || !issuer.Siren.All(char.IsAsciiDigit))
        {
            yield return $"the issuer's SIREN '{issuer.Siren}' is not nine digits";
        }

        if (string.IsNullOrWhiteSpace(issuer.VatNumber))
        {
            yield return "the issuer has no VAT number";
        }

        if (issuer.Address.Count == 0)
        {
            yield return "the issuer has no address";
        }
    }
}

public sealed class CustomerValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document)
    {
        var customer = document.Customer;
        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            yield return "the customer has no name";
        }

        if (customer.Address.Count == 0)
        {
            yield return "the customer has no address";
        }

        if ((customer.Siren is null) != (customer.VatNumber is null))
        {
            yield return "the customer has a SIREN and no VAT number, or the reverse";
        }
    }
}

public sealed class LinesValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document)
    {
        if (document.Lines.Count == 0)
        {
            yield return "the document has no line";
        }

        foreach (var (line, position) in document.Lines.Select((line, index) => (line, index + 1)))
        {
            if (string.IsNullOrWhiteSpace(line.Designation))
            {
                yield return $"line {position} has no designation";
            }

            string?[] amounts = [line.Quantity, line.UnitPriceExclVat, line.DiscountRate, line.NetUnitPriceExclVat, line.TotalExclVat, line.VatRate];
            if (!amounts.All(amount => amount is null || Amounts.TryRead(amount, out _)))
            {
                yield return $"line {position} has an amount that is not a number";
            }
        }
    }
}

// The totals add up: by addition only, which is what the reader of the PDF can check by hand.
public sealed class TotalsValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document)
    {
        var totals = document.Totals;
        if (!Amounts.TryRead(totals.ExclVat, out var excludingVat) || !Amounts.TryRead(totals.Vat, out var vat) || !Amounts.TryRead(totals.InclVat, out var includingVat))
        {
            yield return "a total is not a number";
            yield break;
        }

        if (Sum(document.Lines.Select(line => line.TotalExclVat)) is { } lines && lines != excludingVat)
        {
            yield return "the lines do not add up to the total excluding VAT";
        }

        if (Sum(document.VatBreakdown.Select(rate => rate.BaseExclVat)) != excludingVat || Sum(document.VatBreakdown.Select(rate => rate.Vat)) != vat)
        {
            yield return "the VAT breakdown does not add up to the totals";
        }

        if (includingVat != excludingVat + vat)
        {
            yield return "the total including VAT is not the total excluding VAT plus the VAT";
        }
    }

    // Null when an amount is unreadable: the lines validator already says so.
    private static decimal? Sum(IEnumerable<string> amounts)
    {
        decimal sum = 0m;
        foreach (var amount in amounts)
        {
            if (!Amounts.TryRead(amount, out var value))
            {
                return null;
            }

            sum += value;
        }

        return sum;
    }
}

// The mentions a quote cannot go without, checked apart from the rules that put them.
public sealed class MentionsValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document)
    {
        var codes = document.Mentions.Select(mention => mention.Code).ToHashSet();
        var required = new List<MentionCode> { MentionCode.IssuerLegalIdentity, MentionCode.QuoteValidity };
        if (document.Lines.Any(line => Amounts.TryRead(line.VatRate, out var rate) && rate == 10m))
        {
            required.Add(MentionCode.ReducedVatRate);
        }

        foreach (var code in required.Where(code => !codes.Contains(code.ToString())))
        {
            yield return $"the mention {code} is missing";
        }
    }
}

internal static class Amounts
{
    public static bool TryRead(string text, out decimal amount) =>
        decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
}

internal static class Dates
{
    public static bool TryRead(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
