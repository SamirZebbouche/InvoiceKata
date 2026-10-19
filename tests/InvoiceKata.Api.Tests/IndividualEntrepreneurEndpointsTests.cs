using System.Net;
using System.Net.Http.Json;

using InvoiceKata.Domain;

namespace InvoiceKata.Api.Tests;

// The second kind of issuer through the API (ADR-0011): an individual entrepreneur in VAT franchise.
public sealed class IndividualEntrepreneurEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Should_ReturnAnIndividualEntrepreneurInFranchise_When_ConfiguringOne()
    {
        var client = factory.CreateClient();

        var configured = await client.PostAsJsonAsync("/issuers", Entrepreneur);
        var issuer = await client.GetFromJsonAsync<IssuerBody>(configured.Headers.Location);

        Assert.Equal(HttpStatusCode.Created, configured.StatusCode);
        Assert.Equal(("IndividualEntrepreneur", "Franchise", "Jean Dupont", (string?)null, (decimal?)null, (string?)null),
            (issuer!.Kind, issuer.VatRegime, issuer.Name, issuer.LegalForm, issuer.ShareCapital, issuer.VatNumber));
    }

    [Fact]
    public async Task Should_StillReturnACompanyAtTheNormalRegime_When_NoKindIsGiven()
    {
        var client = factory.CreateClient();

        var configured = await client.PostAsJsonAsync("/issuers", new
        {
            name = "Batifolio",
            legalForm = "SAS",
            shareCapital = 20000m,
            siren = "912345678",
            vatNumber = "FR12912345678",
            address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
        });
        var issuer = await client.GetFromJsonAsync<IssuerBody>(configured.Headers.Location);

        Assert.Equal(("Company", "Normal", "SAS"), (issuer!.Kind, issuer.VatRegime, issuer.LegalForm));
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_ACompanyLacksItsLegalFormCapitalOrVatNumber()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/issuers", new
        {
            kind = "Company",
            name = "Batifolio",
            siren = "912345678",
            address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Incomplete company", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_TheIssuerKindIsUnknown()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/issuers", new
        {
            kind = "Trust",
            name = "Fiducie",
            siren = "912345678",
            address = new { street = "1 rue", postalCode = "47000", city = "Agen" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Unknown issuer kind", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    public static TheoryData<IssuerKind> AllIssuerKinds => [.. Enum.GetValues<IssuerKind>()];

    // A kind added to the domain but not to the controller would fall into its throwing default: this fails that day.
    [Theory]
    [MemberData(nameof(AllIssuerKinds))]
    public async Task Should_ConfigureTheIssuer_When_GivenAnyIssuerKind(IssuerKind kind)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/issuers", new
        {
            kind = kind.ToString(),
            name = "Émetteur",
            legalForm = "SAS",
            shareCapital = 20000m,
            siren = "912345678",
            vatNumber = "FR12912345678",
            address = new { street = "1 rue", postalCode = "47000", city = "Agen" },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnprocessableEntity_When_AFranchiseQuoteGetsALineWithAVatRate()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftAsync(client);

        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", Line(vatRate: 20m));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnAQuoteWithoutVatAndWithTheFranchiseMention_When_TheLineHasNoVatRate()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftAsync(client);

        var added = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", Line(vatRate: 0m));

        var quote = await added.Content.ReadFromJsonAsync<QuoteBody>();
        Assert.Equal((1600m, 0m, 1600m), (quote!.Totals.ExcludingVat, quote.Totals.Vat, quote.Totals.IncludingVat));
        Assert.Contains(quote.Mentions, mention => mention.Code == "VatFranchise");
    }

    private static readonly object Entrepreneur = new
    {
        kind = "IndividualEntrepreneur",
        name = "Jean Dupont",
        siren = "853123456",
        address = new { street = "8 chemin des Vignes", postalCode = "47000", city = "Agen" },
    };

    private static object Line(decimal vatRate) => new { designation = "Audit", quantity = 2m, unit = "jour", unitPriceExcludingVat = 800m, vatRate };

    private static async Task<(Guid IssuerId, Guid QuoteId)> DraftAsync(HttpClient client)
    {
        var issuerId = IdFrom(await client.PostAsJsonAsync("/issuers", Entrepreneur));
        var customerId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/customers", new
        {
            name = "Les Toits Rouges SARL",
            siren = "834567890",
            vatNumber = "FR45834567890",
            address = new { street = "4 place du Marché", postalCode = "33000", city = "Bordeaux" },
        }));
        var quoteId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes", new { customerId }));
        return (issuerId, quoteId);
    }

    private static Guid IdFrom(HttpResponseMessage created) => Guid.Parse(created.Headers.Location!.OriginalString.Split('/')[^1]);

    private sealed record IssuerBody(string Kind, string VatRegime, string Name, string? LegalForm, decimal? ShareCapital, string? VatNumber);

    private sealed record TotalsBody(decimal ExcludingVat, decimal Vat, decimal IncludingVat);

    private sealed record MentionBody(string Code, string Text);

    private sealed record QuoteBody(TotalsBody Totals, IReadOnlyList<MentionBody> Mentions);
}
