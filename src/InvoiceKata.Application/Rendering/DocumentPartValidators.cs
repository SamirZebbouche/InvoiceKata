using System.Globalization;

using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

// The validators of each part of a resolved document (ADR-0005, decision 9). They check the contract
// independently of the code that resolved the document: a defect upstream cannot reach a PDF.
// The header of each kind of document: a quote is valid until a date and has an acceptance box; an invoice refers to
// its quote and has none.
public sealed class HeaderValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document) => document.Document.Kind switch
    {
        "quote" => QuoteViolations(document.Document),
        "invoice" => InvoiceViolations(document.Document),
        _ => [$"the document kind '{document.Document.Kind}' is unknown"],
    };

    private static IEnumerable<string> QuoteViolations(DocumentHeader header)
    {
        if (NumberViolation(header, number => QuoteNumber.TryParse(number, out _), "a quote number") is { } number)
        {
            yield return number;
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

    private static IEnumerable<string> InvoiceViolations(DocumentHeader header)
    {
        if (NumberViolation(header, number => InvoiceNumber.TryParse(number, out _), "an invoice number") is { } number)
        {
            yield return number;
        }

        if (!Dates.TryRead(header.IssuedOn, out _))
        {
            yield return $"the issue date '{header.IssuedOn}' is not a date";
        }

        if (header.Quote is null)
        {
            yield return "an invoice refers to no quote";
        }
        else if (!QuoteNumber.TryParse(header.Quote.Number, out _) || !Dates.TryRead(header.Quote.IssuedOn, out _))
        {
            yield return $"the invoice refers to '{header.Quote.Number}' dated '{header.Quote.IssuedOn}', not to a quote";
        }

        if (header.AcceptanceBox)
        {
            yield return "an invoice has no acceptance box";
        }
    }

    private static string? NumberViolation(DocumentHeader header, Func<string, bool> isNumber, string expected) =>
        header.Number.Length == 0 ? "the document has no number"
        : isNumber(header.Number) ? null
        : $"number '{header.Number}' is not {expected}";
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

        if (issuer.Kind is not ("company" or "individual-entrepreneur"))
        {
            yield return $"the issuer kind '{issuer.Kind}' is unknown";
        }

        if (issuer.VatRegime is not ("normal" or "franchise"))
        {
            yield return $"the VAT regime '{issuer.VatRegime}' is unknown";
        }

        // An individual entrepreneur in franchise bills no VAT, and has no VAT number to show (ADR-0011).
        if (issuer.Kind == "company" && string.IsNullOrWhiteSpace(issuer.VatNumber))
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

        // In franchise there is no VAT at all (ADR-0011); otherwise the breakdown adds up to the totals.
        if (document.Issuer.VatRegime == "franchise")
        {
            if (document.VatBreakdown.Count > 0 || vat != 0m)
            {
                yield return "a document in VAT franchise has VAT";
            }
        }
        else if (Sum(document.VatBreakdown.Select(rate => rate.BaseExclVat)) != excludingVat || Sum(document.VatBreakdown.Select(rate => rate.Vat)) != vat)
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

// The mentions a quote or an invoice cannot go without, checked apart from the code that put them.
public sealed class MentionsValidator : IDocumentPartValidator
{
    public IEnumerable<string> ViolationsIn(ResolvedDocument document)
    {
        var codes = document.Mentions.Select(mention => mention.Code).ToHashSet();
        var required = new List<MentionCode> { MentionCode.IssuerLegalIdentity };
        required.AddRange(document.Document.Kind == "invoice"
            ? [MentionCode.PaymentDueDate, MentionCode.EarlyPaymentDiscount, MentionCode.LatePaymentPenalties, MentionCode.RecoveryIndemnity]
            : [MentionCode.QuoteValidity]);
        if (document.Issuer.VatRegime == "franchise")
        {
            required.Add(MentionCode.VatFranchise);
        }

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
