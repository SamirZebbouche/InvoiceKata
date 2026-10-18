using InvoiceKata.Domain;

namespace InvoiceKata.Application.Customers;

public sealed class GetCustomer(ICustomerRepository customers)
{
    public Task<Customer> HandleAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken) =>
        customers.GetAsync(issuerId, customerId, cancellationToken);
}
