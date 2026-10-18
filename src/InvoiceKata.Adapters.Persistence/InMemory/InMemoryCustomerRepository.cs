using System.Collections.Concurrent;

using InvoiceKata.Application.Customers;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<CustomerId, Customer> _customers = new();

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task<Customer> GetAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken) =>
        _customers.TryGetValue(customerId, out var customer) && customer.IssuerId == issuerId
            ? Task.FromResult(customer)
            : Task.FromException<Customer>(new CustomerNotFoundException(issuerId, customerId));
}
