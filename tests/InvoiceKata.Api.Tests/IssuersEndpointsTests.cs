using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceKata.Api.Tests;

public sealed class IssuersEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly object Batifolio = new
    {
        name = "Batifolio",
        legalForm = "SAS",
        shareCapital = 20000m,
        siren = "912345678",
        vatNumber = "FR12912345678",
        address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
    };

    [Fact]
    public async Task Should_ReturnTheIssuerAtItsLocation_When_ConfiguringAnIssuer()
    {
        var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync("/issuers", Batifolio);
        var read = await client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var issuer = await read.Content.ReadFromJsonAsync<IssuerBody>();
        Assert.Equal("Batifolio", issuer!.Name);
        Assert.Equal("912345678", issuer.Siren);
        Assert.Equal($"/issuers/{issuer.Id}", created.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Should_ReturnNotFoundAsProblemDetails_When_IssuerIsUnknown()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/issuers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnBadRequestAsProblemDetails_When_LegalFormIsUnknown()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/issuers", new
        {
            name = "Batifolio",
            legalForm = "XYZ",
            shareCapital = 20000m,
            siren = "912345678",
            vatNumber = "FR12912345678",
            address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnBadRequestAsProblemDetails_When_ARequiredFieldIsMissing()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/issuers", new
        {
            legalForm = "SAS",
            shareCapital = 20000m,
            siren = "912345678",
            vatNumber = "FR12912345678",
            address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnTheLegalFormAsSent_When_ReadingTheIssuer()
    {
        var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync("/issuers", Batifolio);
        var issuer = await client.GetFromJsonAsync<IssuerBody>(created.Headers.Location);

        Assert.Equal("SAS", issuer!.LegalForm);
    }

    private sealed record IssuerBody(Guid Id, string Name, string LegalForm, string Siren);
}
