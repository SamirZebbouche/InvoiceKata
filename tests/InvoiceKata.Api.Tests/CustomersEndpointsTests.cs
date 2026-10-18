using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceKata.Api.Tests;

public sealed class CustomersEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly object LesToitsRouges = new
    {
        name = "Les Toits Rouges SARL",
        siren = "834567890",
        vatNumber = "FR45834567890",
        address = new { street = "4 place du Marché", postalCode = "33000", city = "Bordeaux" },
    };

    [Fact]
    public async Task Should_ReturnTheCustomerAtItsLocation_When_RegisteringAProfessionalCustomer()
    {
        var client = factory.CreateClient();
        var issuerId = await ConfigureIssuerAsync(client);

        var created = await client.PostAsJsonAsync($"/issuers/{issuerId}/customers", LesToitsRouges);
        var read = await client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var customer = await read.Content.ReadFromJsonAsync<CustomerBody>();
        Assert.Equal("Les Toits Rouges SARL", customer!.Name);
        Assert.Equal("Professional", customer.Kind);
        Assert.Equal($"/issuers/{issuerId}/customers/{customer.Id}", created.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_AnotherIssuerReadsTheCustomer()
    {
        var client = factory.CreateClient();
        var owner = await ConfigureIssuerAsync(client);
        var other = await ConfigureIssuerAsync(client);
        var created = await client.PostAsJsonAsync($"/issuers/{owner}/customers", LesToitsRouges);
        var customerId = created.Headers.Location!.OriginalString.Split('/')[^1];

        var response = await client.GetAsync($"/issuers/{other}/customers/{customerId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_RegisteringACustomerForAnUnknownIssuer()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/issuers/{Guid.NewGuid()}/customers", LesToitsRouges);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<Guid> ConfigureIssuerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/issuers", new
        {
            name = "Batifolio",
            legalForm = "SAS",
            shareCapital = 20000m,
            siren = "912345678",
            vatNumber = "FR12912345678",
            address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
        });
        return Guid.Parse(response.Headers.Location!.OriginalString.Split('/')[^1]);
    }

    private sealed record CustomerBody(Guid Id, string Kind, string Name);
}
