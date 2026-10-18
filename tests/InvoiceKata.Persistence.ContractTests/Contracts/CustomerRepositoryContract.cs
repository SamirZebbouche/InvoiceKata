using InvoiceKata.Application.Customers;
using InvoiceKata.Domain;

namespace InvoiceKata.Persistence.ContractTests.Contracts;

public abstract class CustomerRepositoryContract
{
    protected abstract ICustomerRepository CreateRepository();

    public static TheoryData<CustomerKind> CustomerKinds => [.. Enum.GetValues<CustomerKind>()];

    [Theory]
    [MemberData(nameof(CustomerKinds))]
    public async Task Should_GiveBackTheSameCustomer_When_ItWasAdded(CustomerKind kind)
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        var customer = kind switch
        {
            CustomerKind.Professional => Samples.ProfessionalOf(issuer),
            CustomerKind.Individual => Samples.IndividualOf(issuer),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Customer kind without a sample."),
        };

        await repository.AddAsync(customer, CancellationToken.None);
        var found = await repository.GetAsync(issuer.Id, customer.Id, CancellationToken.None);

        Assert.Equal(
            (customer.Id, customer.IssuerId, customer.Kind, customer.Name, customer.Siren, customer.VatNumber, customer.Address),
            (found.Id, found.IssuerId, found.Kind, found.Name, found.Siren, found.VatNumber, found.Address));
    }

    [Fact]
    public async Task Should_RefuseToGiveACustomer_When_AnotherIssuerAsksForIt()
    {
        var repository = CreateRepository();
        var customer = Samples.ProfessionalOf(Samples.Issuer());
        await repository.AddAsync(customer, CancellationToken.None);

        await Assert.ThrowsAsync<CustomerNotFoundException>(() =>
            repository.GetAsync(IssuerId.New(), customer.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Should_RefuseToGiveACustomer_When_ItWasNeverAdded()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<CustomerNotFoundException>(() =>
            repository.GetAsync(IssuerId.New(), CustomerId.New(), CancellationToken.None));
    }
}
