using InvoiceKata.Api.Issuers;
using InvoiceKata.Application.Customers;
using InvoiceKata.Domain;

namespace InvoiceKata.Api.Customers;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder routes)
    {
        var customers = routes.MapGroup("/issuers/{issuerId:guid}/customers");

        customers.MapPost("/", async (Guid issuerId, RegisterCustomerRequest request, RegisterCustomer registerCustomer, CancellationToken cancellationToken) =>
        {
            var id = await registerCustomer.HandleAsync(new IssuerId(issuerId), request.ToCommand(), cancellationToken);
            return Results.Created($"/issuers/{issuerId}/customers/{id.Value}", new { id = id.Value });
        });

        customers.MapGet("/{customerId:guid}", async (Guid issuerId, Guid customerId, GetCustomer getCustomer, CancellationToken cancellationToken) =>
        {
            var customer = await getCustomer.HandleAsync(new IssuerId(issuerId), new CustomerId(customerId), cancellationToken);
            return Results.Ok(CustomerResponse.From(customer));
        });

        return routes;
    }
}

public sealed record RegisterCustomerRequest(string Name, string Siren, string VatNumber, AddressBody Address)
{
    public RegisterCustomerCommand ToCommand() => new(Name, Siren, VatNumber, Address.ToAddress());
}

public sealed record CustomerResponse(Guid Id, string Kind, string Name, string Siren, string VatNumber, AddressBody Address)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id.Value, customer.Kind.ToString(), customer.Name, customer.Siren, customer.VatNumber, AddressBody.From(customer.Address));
}
