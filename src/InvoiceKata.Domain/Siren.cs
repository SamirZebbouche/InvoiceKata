namespace InvoiceKata.Domain;

// A company identifier (9 digits). A type of its own so it cannot be swapped with a VAT number.
public readonly record struct Siren(string Value);
