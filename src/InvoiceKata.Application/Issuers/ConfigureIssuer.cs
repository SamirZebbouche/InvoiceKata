using InvoiceKata.Domain;

namespace InvoiceKata.Application.Issuers;

public sealed record ConfigureIssuerCommand(string Name, string LegalForm, decimal ShareCapital, string Siren, string VatNumber, Address Address);

public sealed class ConfigureIssuer(IIssuerRepository issuers)
{
    public async Task<IssuerId> HandleAsync(ConfigureIssuerCommand command, CancellationToken cancellationToken)
    {
        var issuer = Issuer.ConfigureCompany(
            command.Name, command.LegalForm, command.ShareCapital, command.Siren, command.VatNumber, command.Address);
        await issuers.AddAsync(issuer, cancellationToken);
        return issuer.Id;
    }
}
