using InvoiceKata.Domain;

namespace InvoiceKata.Application.Customers;

// Secondary port (ADR-0006). Every read is scoped by issuer, so isolation is the port's contract, not a caller's check.
public interface ICustomerRepository
{
    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    /// <exception cref="CustomerNotFoundException">No customer with this identifier belongs to this issuer.</exception>
    Task<Customer> GetAsync(IssuerId issuerId, CustomerId customerId, CancellationToken cancellationToken);
}
