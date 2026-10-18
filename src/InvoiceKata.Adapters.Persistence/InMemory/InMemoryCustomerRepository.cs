using System.Collections.Concurrent;

using InvoiceKata.Application.Customers;
using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.InMemory;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, CustomerState> _customers = new();

    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _customers[customer.Id.Value] = customer.ToState();
        return Task.CompletedTask;
    }

    public Task<Customer> GetAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken) =>
        _customers.TryGetValue(customerId.Value, out var state) && state.IssuerId == issuerId.Value
            ? Task.FromResult(Customer.FromState(state))
            : Task.FromException<Customer>(new CustomerNotFoundException(issuerId, customerId));
}
