using InvoiceKata.Domain;

namespace InvoiceKata.Application.Quotes.Mentions;

// The single place where mention texts are written: the future translation table (language rule of CLAUDE.md).
public interface IMentionTexts
{
    string IssuerLegalIdentity(Issuer issuer);

    string ReducedVatRate();

    string QuoteValidity(int validityDays, DateOnly validUntil);

    string InvoiceDueDate(DateOnly dueOn);

    string NoEarlyPaymentDiscount();

    string LatePaymentPenalties();

    string RecoveryIndemnity();
}
