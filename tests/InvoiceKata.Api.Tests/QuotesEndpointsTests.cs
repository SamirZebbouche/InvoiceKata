using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceKata.Api.Tests;

public sealed class QuotesEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Should_ReturnAnEmptyDraftAtItsLocation_When_CreatingAQuote()
    {
        var client = factory.CreateClient();
        var issuerId = await ConfigureIssuerAsync(client);
        var customerId = await RegisterCustomerAsync(client, issuerId);

        var created = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes", new { customerId });
        var read = await client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var quote = await read.Content.ReadFromJsonAsync<QuoteBody>();
        Assert.Equal("Draft", quote!.Status);
        Assert.Equal(customerId, quote.CustomerId);
        Assert.Empty(quote.Lines);
        Assert.Equal($"/issuers/{issuerId}/quotes/{quote.Id}", created.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CreatingAQuoteForAnotherIssuersCustomer()
    {
        var client = factory.CreateClient();
        var owner = await ConfigureIssuerAsync(client);
        var other = await ConfigureIssuerAsync(client);
        var customerId = await RegisterCustomerAsync(client, owner);

        var response = await client.PostAsJsonAsync($"/issuers/{other}/quotes", new { customerId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnTheRecalculatedQuote_When_AddingALine()
    {
        var client = factory.CreateClient();
        var issuerId = await ConfigureIssuerAsync(client);
        var customerId = await RegisterCustomerAsync(client, issuerId);
        var quoteId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes", new { customerId }));

        var added = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", new
        {
            designation = "Audit",
            quantity = 2m,
            unit = "jour",
            unitPriceExcludingVat = 800m,
            vatRate = 20m,
        });

        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var quote = await added.Content.ReadFromJsonAsync<QuoteWithTotalsBody>();
        var line = Assert.Single(quote!.Lines);
        Assert.Equal($"/issuers/{issuerId}/quotes/{quoteId}/lines/{line.Id}", added.Headers.Location!.OriginalString);
        Assert.Equal(1600m, line.TotalExcludingVat);
        Assert.Equal(new TotalsBody(1600m, 320m, 1920m), quote.Totals);
        Assert.Equal([new VatBreakdownBody(20m, 1600m, 320m)], quote.VatBreakdown);
    }

    [Fact]
    public async Task Should_ReturnTheRecalculatedQuote_When_RemovingALine()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);
        var formation = await AddLineAsync(client, issuerId, quoteId, "Formation", 500m);

        var removed = await client.DeleteAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines/{formation}");

        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        var quote = await removed.Content.ReadFromJsonAsync<QuoteWithTotalsBody>();
        Assert.Single(quote!.Lines);
        Assert.Equal(new TotalsBody(800m, 160m, 960m), quote.Totals);
    }

    [Fact]
    public async Task Should_ReturnNotFoundAsProblemDetails_When_RemovingAnUnknownLine()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);

        var response = await client.DeleteAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ApplyTheDiscountWithoutRoundingTheNetPrice_When_AddingADiscountedLine()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);

        var added = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", new
        {
            designation = "Faïence",
            quantity = 18.72m,
            unit = "u",
            unitPriceExcludingVat = 33.33m,
            discountRate = 10m,
            vatRate = 10m,
        });

        var line = Assert.Single((await added.Content.ReadFromJsonAsync<DiscountedQuoteBody>())!.Lines);
        Assert.Equal(10m, line.DiscountRate);
        Assert.Equal(29.997m, line.NetUnitPriceExcludingVat);
        Assert.Equal(561.54m, line.TotalExcludingVat);
    }

    [Fact]
    public async Task Should_ReturnUnprocessableEntityAsProblemDetails_When_QuotingAnIndividualCustomer()
    {
        var client = factory.CreateClient();
        var issuerId = await ConfigureIssuerAsync(client);
        var customerId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/customers", new
        {
            kind = "Individual",
            name = "Jeanne Martin",
            address = new { street = "3 rue des Lilas", postalCode = "33000", city = "Bordeaux" },
        }));

        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes", new { customerId });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ExposeTheIssuerLegalIdentityMention_When_ReadingANewQuote()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);

        var quote = await client.GetFromJsonAsync<QuoteWithMentionsBody>($"/issuers/{issuerId}/quotes/{quoteId}");

        var mention = Assert.Single(quote!.Mentions);
        Assert.Equal("IssuerLegalIdentity", mention.Code);
        Assert.StartsWith("Batifolio, SAS au capital de", mention.Text, StringComparison.Ordinal);
    }

    private static async Task<(Guid IssuerId, Guid QuoteId)> DraftQuoteAsync(HttpClient client)
    {
        var issuerId = await ConfigureIssuerAsync(client);
        var customerId = await RegisterCustomerAsync(client, issuerId);
        var quoteId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes", new { customerId }));
        return (issuerId, quoteId);
    }

    private static async Task<Guid> AddLineAsync(HttpClient client, Guid issuerId, Guid quoteId, string designation, decimal unitPrice)
    {
        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", new
        {
            designation,
            quantity = 1m,
            unit = "forfait",
            unitPriceExcludingVat = unitPrice,
            vatRate = 20m,
        });
        return IdFrom(response);
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
        return IdFrom(response);
    }

    private static async Task<Guid> RegisterCustomerAsync(HttpClient client, Guid issuerId)
    {
        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/customers", new
        {
            name = "Les Toits Rouges SARL",
            siren = "834567890",
            vatNumber = "FR45834567890",
            address = new { street = "4 place du Marché", postalCode = "33000", city = "Bordeaux" },
        });
        return IdFrom(response);
    }

    private static Guid IdFrom(HttpResponseMessage created) => Guid.Parse(created.Headers.Location!.OriginalString.Split('/')[^1]);

    private sealed record QuoteBody(Guid Id, Guid CustomerId, string Status, IReadOnlyList<object> Lines);

    private sealed record LineBody(Guid Id, decimal TotalExcludingVat);

    private sealed record TotalsBody(decimal ExcludingVat, decimal Vat, decimal IncludingVat);

    private sealed record VatBreakdownBody(decimal Rate, decimal BaseExcludingVat, decimal Vat);

    private sealed record DiscountedLineBody(decimal DiscountRate, decimal NetUnitPriceExcludingVat, decimal TotalExcludingVat);

    private sealed record DiscountedQuoteBody(IReadOnlyList<DiscountedLineBody> Lines);

    private sealed record MentionBody(string Code, string Text);

    private sealed record QuoteWithMentionsBody(IReadOnlyList<MentionBody> Mentions);

    private sealed record QuoteWithTotalsBody(IReadOnlyList<LineBody> Lines, TotalsBody Totals, IReadOnlyList<VatBreakdownBody> VatBreakdown);
}
