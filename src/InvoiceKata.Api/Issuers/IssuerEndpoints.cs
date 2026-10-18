using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Api.Issuers;

public static class IssuerEndpoints
{
    public static IEndpointRouteBuilder MapIssuerEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/issuers", async (ConfigureIssuerRequest request, ConfigureIssuer configureIssuer, CancellationToken cancellationToken) =>
        {
            var id = await configureIssuer.HandleAsync(request.ToCommand(), cancellationToken);
            return Results.Created($"/issuers/{id.Value}", new { id = id.Value });
        });

        routes.MapGet("/issuers/{issuerId:guid}", async (Guid issuerId, GetIssuer getIssuer, CancellationToken cancellationToken) =>
        {
            var issuer = await getIssuer.HandleAsync(new IssuerId(issuerId), cancellationToken);
            return Results.Ok(IssuerResponse.From(issuer));
        });

        return routes;
    }
}

public sealed record AddressBody(string Street, string PostalCode, string City)
{
    public Address ToAddress() => new() { Street = Street, PostalCode = PostalCode, City = City };

    public static AddressBody From(Address address) => new(address.Street, address.PostalCode, address.City);
}

public sealed record ConfigureIssuerRequest(string Name, string LegalForm, decimal ShareCapital, string Siren, string VatNumber, AddressBody Address)
{
    public ConfigureIssuerCommand ToCommand() => new(Name, LegalForm, ShareCapital, new Siren(Siren), new VatNumber(VatNumber), Address.ToAddress());
}

public sealed record IssuerResponse(Guid Id, string Name, string LegalForm, decimal ShareCapital, string Siren, string VatNumber, AddressBody Address)
{
    public static IssuerResponse From(Issuer issuer) =>
        new(issuer.Id.Value, issuer.Name, issuer.LegalForm, issuer.ShareCapital, issuer.Siren.Value, issuer.VatNumber.Value, AddressBody.From(issuer.Address));
}
