namespace InvoiceKata.Domain;

// An intra-community VAT number. A type of its own so it cannot be swapped with a SIREN.
public readonly record struct VatNumber(string Value);
