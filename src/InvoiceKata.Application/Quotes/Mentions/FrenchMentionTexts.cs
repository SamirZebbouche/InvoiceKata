using System.Globalization;

using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

public sealed class FrenchMentionTexts : IMentionTexts
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public string IssuerLegalIdentity(Issuer issuer) =>
        $"{issuer.Name}, {LegalFormLabels.Of(issuer.LegalForm)} au capital de {issuer.ShareCapital.ToString("#,0", French)}\u00A0€, "
        + $"SIREN {Grouped(issuer.Siren.Value)}";

    // "912345678" reads "912 345 678".
    private static string Grouped(string siren) => string.Join('\u00A0', siren.Chunk(3).Select(digits => new string(digits)));
}
