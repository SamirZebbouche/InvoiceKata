namespace InvoiceKata.Domain;

// A type of its own so a quantity cannot be swapped with a price or a VAT rate.
public readonly record struct Quantity(decimal Value);
