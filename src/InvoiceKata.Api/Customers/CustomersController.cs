using InvoiceKata.Api.Issuers;
using InvoiceKata.Application.Customers;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Mvc;

namespace InvoiceKata.Api.Customers;

[ApiController]
[Route("issuers/{issuerId:guid}/customers")]
public sealed class CustomersController(RegisterCustomer registerCustomer, GetCustomer getCustomer) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterAsync(Guid issuerId, RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var id = await registerCustomer.HandleAsync(new IssuerId(issuerId), request.ToCommand(), cancellationToken);
        return Created($"/issuers/{issuerId}/customers/{id.Value}", new { id = id.Value });
    }

    [HttpGet("{customerId:guid}")]
    public async Task<CustomerResponse> GetAsync(Guid issuerId, Guid customerId, CancellationToken cancellationToken) =>
        CustomerResponse.From(await getCustomer.HandleAsync(new IssuerId(issuerId), new CustomerId(customerId), cancellationToken));
}

public sealed record RegisterCustomerRequest(string Name, string Siren, string VatNumber, AddressBody Address)
{
    public RegisterCustomerCommand ToCommand() => new(Name, new Siren(Siren), new VatNumber(VatNumber), Address.ToAddress());
}

public sealed record CustomerResponse(Guid Id, string Kind, string Name, string Siren, string VatNumber, AddressBody Address)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id.Value, customer.Kind.ToString(), customer.Name, customer.Siren.Value, customer.VatNumber.Value, AddressBody.From(customer.Address));
}
