using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Issuers;

public sealed class ConfigureIndividualEntrepreneurTests
{
    [Fact]
    public async Task Should_StoreAnIndividualEntrepreneurInFranchise_When_Configured()
    {
        var issuers = new AddedIssuers();
        var command = new ConfigureIndividualEntrepreneurCommand(
            "Jean Dupont", new Siren("853123456"), new Address { Street = "8 chemin des Vignes", PostalCode = "47000", City = "Agen" });

        var id = await new ConfigureIndividualEntrepreneur(issuers).HandleAsync(command, CancellationToken.None);

        var issuer = Assert.Single(issuers.Added);
        Assert.Equal((id, IssuerKind.IndividualEntrepreneur, VatRegime.Franchise, "Jean Dupont", command.Siren, command.Address),
            (issuer.Id, issuer.Kind, issuer.VatRegime, issuer.Name, issuer.Siren, issuer.Address));
    }

    private sealed class AddedIssuers : IIssuerRepository
    {
        public List<Issuer> Added { get; } = [];

        public Task AddAsync(Issuer issuer, CancellationToken cancellationToken)
        {
            Added.Add(issuer);
            return Task.CompletedTask;
        }

        public Task<Issuer> GetAsync(IssuerId id, CancellationToken cancellationToken) => Task.FromResult(Added.Single(issuer => issuer.Id == id));
    }
}
