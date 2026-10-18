using InvoiceKata.Domain;

namespace InvoiceKata.Application.Issuers;

public sealed class GetIssuer(IIssuerRepository issuers)
{
    public Task<Issuer> HandleAsync(IssuerId id, CancellationToken cancellationToken) =>
        issuers.GetAsync(id, cancellationToken);
}
