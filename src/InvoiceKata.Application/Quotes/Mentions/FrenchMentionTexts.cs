using System.Globalization;

using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

public sealed class FrenchMentionTexts : IMentionTexts
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    // A company states its legal form and capital; an individual entrepreneur, "EI" after the name (R526-27 C. com.).
    public string IssuerLegalIdentity(Issuer issuer) => issuer.Kind switch
    {
        IssuerKind.Company =>
            $"{issuer.Name}, {LegalFormLabels.Of(issuer.LegalForm!.Value)} au capital de {Written(issuer.ShareCapital!.Value)}\u00A0€, "
            + $"SIREN {Grouped(issuer.Siren.Value)}",
        IssuerKind.IndividualEntrepreneur => $"{issuer.Name} EI, SIREN {Grouped(issuer.Siren.Value)}",
        _ => throw new ArgumentOutOfRangeException(nameof(issuer), issuer.Kind, "This kind of issuer has no legal identity text."),
    };

    public string ReducedVatRate() =>
        "Taux de TVA réduit de 10 % appliqué aux travaux d'amélioration d'un logement achevé depuis plus de "
        + "deux ans (art. 279-0 bis du CGI).";

    // Stryker disable once String : equivalent, fr-FR's short date is already dd/MM/yyyy; kept explicit on purpose
    public string QuoteValidity(int validityDays, DateOnly validUntil) =>
        $"Devis valable {validityDays} {(validityDays == 1 ? "jour" : "jours")}, jusqu'au {validUntil.ToString("dd/MM/yyyy", French)}.";

    public string InvoiceDueDate(DateOnly dueOn) => $"Échéance : {dueOn.ToString("dd/MM/yyyy", French)}.";

    public string NoEarlyPaymentDiscount() => "Escompte pour paiement anticipé : néant.";

    // Between professionals, three times the legal interest rate, the minimum the code de commerce sets; face to a consumer,
    // the common law: the legal interest rate, from the formal notice (art. 1231-6 du code civil, ADR-0012).
    public string LatePaymentPenalties(CustomerKind customerKind) => customerKind switch
    {
        CustomerKind.Professional => "Pénalités de retard : trois fois le taux d'intérêt légal.",
        CustomerKind.Individual => "Pénalités de retard : intérêts au taux légal, à compter de la mise en demeure (art. 1231-6 du code civil).",
        _ => throw new ArgumentOutOfRangeException(nameof(customerKind), customerKind, "This kind of customer has no late payment penalties text."),
    };

    public string RecoveryIndemnity() => "Indemnité forfaitaire pour frais de recouvrement : 40 €.";

    // The reference in force since 1 September 2026; "art. 293 B du CGI" is only tolerated until the end of 2027 (ADR-0011).
    public string VatFranchise() => "TVA non applicable, art. L. 223 et s. du code des impositions sur les biens et services (CIBS)";

    // "20 000", but "7 622,45": a capital converted from francs keeps its cents, never rounded.
    private static string Written(decimal shareCapital) =>
        shareCapital.ToString(decimal.IsInteger(shareCapital) ? "#,0" : "#,0.00", French);

    // "912345678" reads "912 345 678".
    private static string Grouped(string siren) => string.Join('\u00A0', siren.Chunk(3).Select(digits => new string(digits)));
}
