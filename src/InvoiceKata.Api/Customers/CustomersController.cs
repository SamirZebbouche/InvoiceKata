using InvoiceKata.Api.Issuers;
using InvoiceKata.Application.Customers;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Mvc;

namespace InvoiceKata.Api.Customers;

[ApiController]
[Route("issuers/{issuerId:guid}/customers")]
public sealed class CustomersController(
    RegisterCustomer registerCustomer, RegisterIndividualCustomer registerIndividualCustomer, GetCustomer getCustomer)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterAsync(Guid issuerId, RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CustomerKind>(request.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind) || int.TryParse(request.Kind, out _))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unknown customer kind",
                detail: $"'{request.Kind}' is not a customer kind; use Professional or Individual.");
        }

        CustomerId id;
        switch (kind)
        {
            case CustomerKind.Professional:
                if (request.Siren is null || request.VatNumber is null)
                {
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Incomplete professional customer",
                        detail: "A professional customer needs a SIREN and a VAT number.");
                }

                id = await registerCustomer.HandleAsync(new IssuerId(issuerId), new RegisterCustomerCommand(
                    request.Name, new Siren(request.Siren), new VatNumber(request.VatNumber), request.Address.ToAddress()), cancellationToken);
                break;
            case CustomerKind.Individual:
                id = await registerIndividualCustomer.HandleAsync(new IssuerId(issuerId), new RegisterIndividualCustomerCommand(
                    request.Name, request.Address.ToAddress()), cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), kind, "Customer kind without a registration.");
        }

        return Created($"/issuers/{issuerId}/customers/{id.Value}", new { id = id.Value });
    }

    [HttpGet("{customerId:guid}")]
    public async Task<CustomerResponse> GetAsync(Guid issuerId, Guid customerId, CancellationToken cancellationToken) =>
        CustomerResponse.From(await getCustomer.HandleAsync(new IssuerId(issuerId), new CustomerId(customerId), cancellationToken));
}

// Kind defaults to Professional, so callers written before individual customers existed keep working.
public sealed record RegisterCustomerRequest(string Name, AddressBody Address, string Kind = nameof(CustomerKind.Professional), string? Siren = null, string? VatNumber = null);

public sealed record CustomerResponse(Guid Id, string Kind, string Name, string? Siren, string? VatNumber, AddressBody Address)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id.Value, customer.Kind.ToString(), customer.Name, customer.Siren?.Value, customer.VatNumber?.Value, AddressBody.From(customer.Address));
}
