using System.Net;
using System.Net.Http.Json;

namespace InvoiceKata.Api.Tests;

public sealed class RestartTests
{
    [Fact]
    public async Task Should_FindTheIssuerAgain_When_TheApiRestartsOnTheSameDataDirectory()
    {
        using var first = new ApiFactory();
        var created = await first.CreateClient().PostAsJsonAsync("/issuers", new
        {
            name = "Batifolio",
            legalForm = "SAS",
            shareCapital = 20000m,
            siren = "912345678",
            vatNumber = "FR12912345678",
            address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
        });

        Assert.True(File.Exists(Path.Combine(first.DataDirectory, "issuers", created.Headers.Location!.OriginalString.Split('/')[^1], "issuer.json")),
            "The API did not write under the configured data directory.");

        using var restarted = ApiFactory.On(first.DataDirectory);
        var read = await restarted.CreateClient().GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }
}
