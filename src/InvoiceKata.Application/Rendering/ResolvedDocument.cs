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
            Of(issuer),
            Of(customer),
            [.. quote.Lines.Select(Of)],
            [.. quote.VatBreakdown.Select(Of)],
            new DocumentTotals(Cents(quote.TotalExcludingVat.Amount), Cents(quote.TotalVat.Amount), Cents(quote.TotalIncludingVat.Amount)),
            [.. quote.Mentions.Select(Of)]);
    }

    public static ResolvedDocument OfInvoice(Invoice invoice, Issuer issuer, Customer customer) => new(
        new DocumentHeader(
            "invoice", invoice.Number.ToString(), Text(invoice.IssuedOn), null, AcceptanceBox: false,
            new QuoteReference(invoice.QuoteNumber.ToString(), Text(invoice.QuoteIssuedOn))),
        Of(issuer),
        Of(customer),
        [.. invoice.Lines.Select(Of)],
        [.. invoice.VatBreakdown.Select(Of)],
        new DocumentTotals(Cents(invoice.TotalExcludingVat.Amount), Cents(invoice.TotalVat.Amount), Cents(invoice.TotalIncludingVat.Amount)),
        [.. invoice.Mentions.Select(Of)]);

    // A company shows its legal form and VAT number; an individual entrepreneur, "EI" and none (ADR-0011).
    private static DocumentIssuer Of(Issuer issuer) => issuer.Kind switch
    {
        IssuerKind.Company => new(
            issuer.Name, LegalFormLabels.Of(issuer.LegalForm!.Value), issuer.Siren.Value, issuer.VatNumber!.Value.Value, AddressLines(issuer.Address),
            "company", Regime(issuer.VatRegime)),
        IssuerKind.IndividualEntrepreneur => new(
            issuer.Name, "EI", issuer.Siren.Value, null, AddressLines(issuer.Address), "individual-entrepreneur", Regime(issuer.VatRegime)),
        _ => throw new ArgumentOutOfRangeException(nameof(issuer), issuer.Kind, "This kind of issuer has no document."),
    };

    private static string Regime(VatRegime regime) => regime switch
    {
        VatRegime.Normal => "normal",
        VatRegime.Franchise => "franchise",
        _ => throw new ArgumentOutOfRangeException(nameof(regime), regime, "This VAT regime has no document."),
    };

    private static DocumentCustomer Of(Customer customer) => new(customer.Name, AddressLines(customer.Address), customer.Siren?.Value, customer.VatNumber?.Value);

    private static DocumentLine Of(QuoteLine line) => new(
        line.Designation,
        Text(line.Quantity.Value),
        line.Unit,
        Text(line.UnitPriceExcludingVat.Amount),
        line.Discount == DiscountRate.None ? null : Text(line.Discount.Percent),
        Text(line.NetUnitPriceExcludingVat.Amount),
        Cents(line.TotalExcludingVat.Amount),
        Text(line.VatRate.Percent));

    private static DocumentVatRate Of(VatBreakdownLine rate) => new(Text(rate.Rate.Percent), Cents(rate.BaseExcludingVat.Amount), Cents(rate.Vat.Amount));

    private static DocumentMention Of(LegalMention mention) => new(mention.Code.ToString(), mention.Text);

    private static IReadOnlyList<string> AddressLines(Address address) => [address.Street, $"{address.PostalCode} {address.City}"];

    // Inputs keep their full precision; legal amounts are already rounded to the cent, and say so (ADR-0008).
    private static string Text(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Cents(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Text(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
}

public sealed record DocumentHeader(string Kind, string Number, string IssuedOn, string? ValidUntil, bool AcceptanceBox, QuoteReference? Quote = null);

// The quote an invoice was issued from, which the template refers to.
public sealed record QuoteReference(string Number, string IssuedOn);

public sealed record DocumentIssuer(
    string Name, string LegalForm, string Siren, string? VatNumber, IReadOnlyList<string> Address, string Kind = "company", string VatRegime = "normal");

public sealed record DocumentCustomer(string Name, IReadOnlyList<string> Address, string? Siren, string? VatNumber);

public sealed record DocumentLine(
    string Designation, string Quantity, string Unit, string UnitPriceExclVat, string? DiscountRate, string NetUnitPriceExclVat, string TotalExclVat, string VatRate);

public sealed record DocumentVatRate(string Rate, string BaseExclVat, string Vat);

public sealed record DocumentTotals(string ExclVat, string Vat, string InclVat);

public sealed record DocumentMention(string Code, string Text);
