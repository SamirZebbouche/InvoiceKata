using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Persistence.ContractTests.Contracts;

// One suite per port, replayed on every adapter (ADR-0006): an adapter that passes it is interchangeable.
public abstract class IssuerRepositoryContract
{
    protected abstract IIssuerRepository CreateRepository();

    [Fact]
    public async Task Should_GiveBackTheSameIssuer_When_ItWasAdded()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();

        await repository.AddAsync(issuer, CancellationToken.None);
        var found = await repository.GetAsync(issuer.Id, CancellationToken.None);

        Assert.Equal(
            (issuer.Id, issuer.Name, issuer.LegalForm, issuer.ShareCapital, issuer.Siren, issuer.VatNumber, issuer.Address),
            (found.Id, found.Name, found.LegalForm, found.ShareCapital, found.Siren, found.VatNumber, found.Address));
    }

    [Fact]
    public async Task Should_RefuseToGiveAnIssuer_When_ItWasNeverAdded()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<IssuerNotFoundException>(() => repository.GetAsync(IssuerId.New(), CancellationToken.None));
    }
}
