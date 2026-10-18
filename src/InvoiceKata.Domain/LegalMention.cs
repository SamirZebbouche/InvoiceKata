namespace InvoiceKata.Domain;

// The code identifies a mention (and will map to Factur-X coded notes); the text comes from the text catalog (CDR-0001).
public enum MentionCode
{
    IssuerLegalIdentity,
    ReducedVatRate,
    QuoteValidity,
}

public sealed record LegalMention(MentionCode Code, string Text);
