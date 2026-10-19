namespace InvoiceKata.Domain;

// How an issuer bills VAT (ADR-0011): a company at the normal regime, an individual entrepreneur in franchise.
public enum VatRegime
{
    Normal,
    Franchise,
}
