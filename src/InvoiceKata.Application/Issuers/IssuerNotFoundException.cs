using InvoiceKata.Domain;

namespace InvoiceKata.Application.Issuers;

public sealed class IssuerNotFoundException(IssuerId id) : ResourceNotFoundException($"Issuer {id.Value} was not found.")
{
    public IssuerId IssuerId { get; } = id;
}
