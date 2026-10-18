namespace InvoiceKata.Domain;

// Legal forms of a French company issuer. It will drive the legal mentions and the rendering template.
public enum LegalForm
{
    Sa,
    Sas,
    Sasu,
    Sarl,
    Eurl,
    Snc,
}
