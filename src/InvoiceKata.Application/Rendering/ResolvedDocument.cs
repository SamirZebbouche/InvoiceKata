using System.Globalization;

using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Rendering;

// The contract between the domain and the rendering (ADR-0005): everything is already computed and resolved,
// the template only lays it out. Amounts are invariant strings, which the template reads as decimals.
// No text for the reader here: the template writes the labels, in its language, from the kind.
public sealed record ResolvedDocument(
    DocumentHeader Document,
    DocumentIssuer Issuer,
    DocumentCustomer Customer,
    IReadOnlyList<DocumentLine> Lines,
    IReadOnlyList<DocumentVatRate> VatBreakdown,
    DocumentTotals Totals,
    IReadOnlyList<DocumentMention> Mentions)
{
    public static ResolvedDocument OfQuote(QuoteScope scope)
    {
        var (quote, issuer, customer) = (scope.Quote, scope.Issuer, scope.Customer);
        return new ResolvedDocument(
            new DocumentHeader("quote", quote.Number?.ToString() ?? "", Text(quote.IssuedOn), quote.ValidUntil is { } validUntil ? Text(validUntil) : null, AcceptanceBox: true),
            new DocumentIssuer(issuer.Name, LegalFormLabels.Of(issuer.LegalForm), issuer.Siren.Value, issuer.VatNumber.Value, AddressLines(issuer.Address)),
            new DocumentCustomer(customer.Name, AddressLines(customer.Address), customer.Siren?.Value, customer.VatNumber?.Value),
            [.. quote.Lines.Select(line => new DocumentLine(
                line.Designation,
                Text(line.Quantity.Value),
                line.Unit,
                Text(line.UnitPriceExcludingVat.Amount),
                line.Discount == DiscountRate.None ? null : Text(line.Discount.Percent),
                Text(line.NetUnitPriceExcludingVat.Amount),
                Cents(line.TotalExcludingVat.Amount),
                Text(line.VatRate.Percent)))],
            [.. quote.VatBreakdown.Select(rate => new DocumentVatRate(Text(rate.Rate.Percent), Cents(rate.BaseExcludingVat.Amount), Cents(rate.Vat.Amount)))],
            new DocumentTotals(Cents(quote.TotalExcludingVat.Amount), Cents(quote.TotalVat.Amount), Cents(quote.TotalIncludingVat.Amount)),
            [.. quote.Mentions.Select(mention => new DocumentMention(mention.Code.ToString(), mention.Text))]);
    }

    private static IReadOnlyList<string> AddressLines(Address address) => [address.Street, $"{address.PostalCode} {address.City}"];

    // Inputs keep their full precision; legal amounts are already rounded to the cent, and say so (ADR-0008).
    private static string Text(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Cents(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Text(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
}

public sealed record DocumentHeader(string Kind, string Number, string IssuedOn, string? ValidUntil, bool AcceptanceBox);

public sealed record DocumentIssuer(string Name, string LegalForm, string Siren, string VatNumber, IReadOnlyList<string> Address);

public sealed record DocumentCustomer(string Name, IReadOnlyList<string> Address, string? Siren, string? VatNumber);

public sealed record DocumentLine(
    string Designation, string Quantity, string Unit, string UnitPriceExclVat, string? DiscountRate, string NetUnitPriceExclVat, string TotalExclVat, string VatRate);

public sealed record DocumentVatRate(string Rate, string BaseExclVat, string Vat);

public sealed record DocumentTotals(string ExclVat, string Vat, string InclVat);

public sealed record DocumentMention(string Code, string Text);
