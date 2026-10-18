using InvoiceKata.Domain;

namespace InvoiceKata.Api.Issuers;

// Translates the domain enum to and from the labels the API exchanges ("SAS", "SARL"…).
public static class LegalFormLabels
{
    public static string Of(LegalForm legalForm) => legalForm switch
    {
        LegalForm.Sa => "SA",
        LegalForm.Sas => "SAS",
        LegalForm.Sasu => "SASU",
        LegalForm.Sarl => "SARL",
        LegalForm.Eurl => "EURL",
        LegalForm.Snc => "SNC",
        _ => throw new ArgumentOutOfRangeException(nameof(legalForm), legalForm, "Legal form without a label."),
    };

    public static bool TryParse(string label, out LegalForm legalForm)
    {
        foreach (var candidate in Enum.GetValues<LegalForm>())
        {
            if (string.Equals(Of(candidate), label, StringComparison.OrdinalIgnoreCase))
            {
                legalForm = candidate;
                return true;
            }
        }

        legalForm = default;
        return false;
    }
}
