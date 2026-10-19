using System.Globalization;

using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

public sealed class FrenchMentionTexts : IMentionTexts
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public string IssuerLegalIdentity(Issuer issuer) =>
        $"{issuer.Name}, {LegalFormLabels.Of(issuer.LegalForm)} au capital de {Written(issuer.ShareCapital)}\u00A0€, "
        + $"SIREN {Grouped(issuer.Siren.Value)}";

    public string ReducedVatRate() =>
        "Taux de TVA réduit de 10 % appliqué aux travaux d'amélioration d'un logement achevé depuis plus de "
        + "deux ans (art. 279-0 bis du CGI).";

    // Stryker disable once String : equivalent, fr-FR's short date is already dd/MM/yyyy; kept explicit on purpose
    public string QuoteValidity(int validityDays, DateOnly validUntil) =>
        $"Devis valable {validityDays} {(validityDays == 1 ? "jour" : "jours")}, jusqu'au {validUntil.ToString("dd/MM/yyyy", French)}.";

    public string InvoiceDueDate(DateOnly dueOn) => $"Échéance : {dueOn.ToString("dd/MM/yyyy", French)}.";

    public string NoEarlyPaymentDiscount() => "Escompte pour paiement anticipé : néant.";

    // Three times the legal interest rate: the minimum the law sets.
    public string LatePaymentPenalties() => "Pénalités de retard : trois fois le taux d'intérêt légal.";

    public string RecoveryIndemnity() => "Indemnité forfaitaire pour frais de recouvrement : 40 €.";

    // "20 000", but "7 622,45": a capital converted from francs keeps its cents, never rounded.
    private static string Written(decimal shareCapital) =>
        shareCapital.ToString(decimal.IsInteger(shareCapital) ? "#,0" : "#,0.00", French);

    // "912345678" reads "912 345 678".
    private static string Grouped(string siren) => string.Join('\u00A0', siren.Chunk(3).Select(digits => new string(digits)));
}
