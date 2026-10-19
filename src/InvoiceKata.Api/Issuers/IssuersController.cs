using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Mvc;

namespace InvoiceKata.Api.Issuers;

[ApiController]
[Route("issuers")]
public sealed class IssuersController(ConfigureIssuer configureIssuer, ConfigureIndividualEntrepreneur configureIndividualEntrepreneur, GetIssuer getIssuer)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ConfigureAsync(ConfigureIssuerRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IssuerKind>(request.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind) || int.TryParse(request.Kind, out _))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unknown issuer kind",
                detail: $"'{request.Kind}' is not an issuer kind; use Company or IndividualEntrepreneur.");
        }

        IssuerId id;
        switch (kind)
        {
            case IssuerKind.Company:
                if (request.LegalForm is null || request.ShareCapital is null || request.VatNumber is null)
                {
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Incomplete company",
                        detail: "A company needs a legal form, a share capital and a VAT number.");
                }

                if (!LegalFormLabels.TryParse(request.LegalForm, out var legalForm))
                {
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unknown legal form",
                        detail: $"'{request.LegalForm}' is not a supported legal form.");
                }

                id = await configureIssuer.HandleAsync(new ConfigureIssuerCommand(
                    request.Name, legalForm, request.ShareCapital.Value, new Siren(request.Siren), new VatNumber(request.VatNumber), request.Address.ToAddress()),
                    cancellationToken);
                break;
            case IssuerKind.IndividualEntrepreneur:
                id = await configureIndividualEntrepreneur.HandleAsync(
                    new ConfigureIndividualEntrepreneurCommand(request.Name, new Siren(request.Siren), request.Address.ToAddress()), cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), kind, "Issuer kind without a configuration.");
        }

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

// Kind defaults to Company, so callers written before individual entrepreneurs existed keep working (ADR-0011).
public sealed record ConfigureIssuerRequest(
    string Name, string Siren, AddressBody Address, string Kind = nameof(IssuerKind.Company), string? LegalForm = null, decimal? ShareCapital = null,
    string? VatNumber = null);

public sealed record IssuerResponse(
    Guid Id, string Kind, string VatRegime, string Name, string? LegalForm, decimal? ShareCapital, string Siren, string? VatNumber, AddressBody Address)
{
    public static IssuerResponse From(Issuer issuer) => new(
        issuer.Id.Value, issuer.Kind.ToString(), issuer.VatRegime.ToString(), issuer.Name,
        issuer.LegalForm is { } legalForm ? LegalFormLabels.Of(legalForm) : null, issuer.ShareCapital, issuer.Siren.Value, issuer.VatNumber?.Value,
        AddressBody.From(issuer.Address));
}
