using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Mvc;

namespace InvoiceKata.Api.Issuers;

[ApiController]
[Route("issuers")]
public sealed class IssuersController(ConfigureIssuer configureIssuer, GetIssuer getIssuer) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ConfigureAsync(ConfigureIssuerRequest request, CancellationToken cancellationToken)
    {
        if (!LegalFormLabels.TryParse(request.LegalForm, out var legalForm))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Unknown legal form",
                detail: $"'{request.LegalForm}' is not a supported legal form.");
        }

        var id = await configureIssuer.HandleAsync(request.ToCommand(legalForm), cancellationToken);
        return Created($"/issuers/{id.Value}", new { id = id.Value });
    }

    [HttpGet("{issuerId:guid}")]
    public async Task<IssuerResponse> GetAsync(Guid issuerId, CancellationToken cancellationToken) =>
        IssuerResponse.From(await getIssuer.HandleAsync(new IssuerId(issuerId), cancellationToken));
}

public sealed record AddressBody(string Street, string PostalCode, string City)
{
    public Address ToAddress() => new() { Street = Street, PostalCode = PostalCode, City = City };

    public static AddressBody From(Address address) => new(address.Street, address.PostalCode, address.City);
}

public sealed record ConfigureIssuerRequest(string Name, string LegalForm, decimal ShareCapital, string Siren, string VatNumber, AddressBody Address)
{
    public ConfigureIssuerCommand ToCommand(LegalForm legalForm) => new(Name, legalForm, ShareCapital, new Siren(Siren), new VatNumber(VatNumber), Address.ToAddress());
}

public sealed record IssuerResponse(Guid Id, string Name, string LegalForm, decimal ShareCapital, string Siren, string VatNumber, AddressBody Address)
{
    public static IssuerResponse From(Issuer issuer) =>
        new(issuer.Id.Value, issuer.Name, LegalFormLabels.Of(issuer.LegalForm), issuer.ShareCapital, issuer.Siren.Value, issuer.VatNumber.Value, AddressBody.From(issuer.Address));
}
