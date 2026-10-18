using InvoiceKata.Domain;

namespace InvoiceKata.Application.Customers;

public sealed class CustomerNotFoundException(IssuerId issuerId, CustomerId customerId)
    : ResourceNotFoundException($"Customer {customerId.Value} was not found for issuer {issuerId.Value}.")
{
    public IssuerId IssuerId { get; } = issuerId;

    public CustomerId CustomerId { get; } = customerId;
}
