using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Customers;

public sealed record RegisterIndividualCustomerCommand(string Name, Address Address);

public sealed class RegisterIndividualCustomer(IIssuerRepository issuers, ICustomerRepository customers)
{
    /// <exception cref="IssuerNotFoundException">The issuer does not exist.</exception>
    public async Task<CustomerId> HandleAsync(IssuerId issuerId, RegisterIndividualCustomerCommand command, CancellationToken cancellationToken)
    {
        var issuer = await issuers.GetAsync(issuerId, cancellationToken);
        var customer = Customer.RegisterIndividual(issuer.Id, command.Name, command.Address);
        await customers.AddAsync(customer, cancellationToken);
        return customer.Id;
    }
}
