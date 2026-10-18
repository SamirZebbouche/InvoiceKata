using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Customers;

public sealed record RegisterCustomerCommand(string Name, Siren Siren, VatNumber VatNumber, Address Address);

public sealed class RegisterCustomer(IIssuerRepository issuers, ICustomerRepository customers)
{
    /// <exception cref="IssuerNotFoundException">The issuer does not exist.</exception>
    public async Task<CustomerId> HandleAsync(IssuerId issuerId, RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var issuer = await issuers.GetAsync(issuerId, cancellationToken);
        var customer = Customer.RegisterProfessional(issuer.Id, command.Name, command.Siren, command.VatNumber, command.Address);
        await customers.AddAsync(customer, cancellationToken);
        return customer.Id;
    }
}
