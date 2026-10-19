using InvoiceKata.Domain;

namespace InvoiceKata.Application.Issuers;

public sealed record ConfigureIndividualEntrepreneurCommand(string Name, Siren Siren, Address Address);

// The second kind of issuer (ADR-0011): an individual entrepreneur, in VAT franchise.
public sealed class ConfigureIndividualEntrepreneur(IIssuerRepository issuers)
{
    public async Task<IssuerId> HandleAsync(ConfigureIndividualEntrepreneurCommand command, CancellationToken cancellationToken)
    {
        var issuer = Issuer.ConfigureIndividualEntrepreneur(command.Name, command.Siren, command.Address);
        await issuers.AddAsync(issuer, cancellationToken);
        return issuer.Id;
    }
}
