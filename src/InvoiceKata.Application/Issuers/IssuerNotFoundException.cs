using InvoiceKata.Domain;

namespace InvoiceKata.Application.Issuers;

public sealed class IssuerNotFoundException(IssuerId id) : Exception($"Issuer {id.Value} was not found.")
{
    public IssuerId IssuerId { get; } = id;
}
