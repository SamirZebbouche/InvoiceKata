using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceKata.Api.Tests;

public sealed class QuotesEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
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

    [Fact]
    public async Task Should_ExposeTheReducedVatRateMention_When_ALineIsAtTenPercent()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);

        var added = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", new
        {
            designation = "Pose de faïence",
            quantity = 1m,
            unit = "forfait",
            unitPriceExcludingVat = 500m,
            vatRate = 10m,
        });

        var quote = await added.Content.ReadFromJsonAsync<QuoteWithMentionsBody>();
        Assert.Contains(quote!.Mentions, mention => mention.Code == "ReducedVatRate");
    }

    [Fact]
    public async Task Should_ReturnTheQuoteWithItsNumberAndDates_When_Finalizing()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);

        var finalized = await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);

        Assert.Equal(HttpStatusCode.OK, finalized.StatusCode);
        var quote = await finalized.Content.ReadFromJsonAsync<FinalizedQuoteBody>();
        Assert.Equal(("Finalized", "D-2026-0001", "2026-10-18", "2026-11-17"), (quote!.Status, quote.Number, quote.IssuedOn, quote.ValidUntil));
        Assert.Contains(new MentionBody("QuoteValidity", "Devis valable 30 jours, jusqu'au 17/11/2026."), quote.Mentions);
        Assert.Equal(quote, await client.GetFromJsonAsync<FinalizedQuoteBody>($"/issuers/{issuerId}/quotes/{quoteId}"), FinalizedQuoteBody.Comparer);
    }

    [Fact]
    public async Task Should_EndTheValidityAfterTheDaysAsked_When_FinalizingWithAValidity()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);

        var finalized = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", new { validityDays = 15 });

        Assert.Equal("2026-11-02", (await finalized.Content.ReadFromJsonAsync<FinalizedQuoteBody>())!.ValidUntil);
    }

    [Fact]
    public async Task Should_HaveNoNumberNorDates_When_StillADraft()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);

        var quote = await client.GetFromJsonAsync<FinalizedQuoteBody>($"/issuers/{issuerId}/quotes/{quoteId}");

        Assert.Equal(((string?)null, (string?)null, (string?)null), (quote!.Number, quote.IssuedOn, quote.ValidUntil));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task Should_ReturnBadRequestAndKeepTheDraft_When_TheValidityIsOutOfRange(int validityDays)
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);

        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", new { validityDays });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Draft", (await client.GetFromJsonAsync<FinalizedQuoteBody>($"/issuers/{issuerId}/quotes/{quoteId}"))!.Status);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_AddingALineToAFinalizedQuote()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);
        await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);

        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", new
        {
            designation = "Formation",
            quantity = 1m,
            unit = "jour",
            unitPriceExcludingVat = 600m,
            vatRate = 20m,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_FinalizingAQuoteWithoutLines()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);

        var response = await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_FinalizingAnotherIssuersQuote()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);
        var other = await ConfigureIssuerAsync(client);

        var response = await client.PostAsync($"/issuers/{other}/quotes/{quoteId}/finalization", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // The PDF is rendered as a reaction to the finalization, in the same request (ADR-0005 amendment).
    [Fact]
    public async Task Should_StoreThePdfNextToTheQuote_When_Finalizing()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);

        await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);

        Assert.True(File.Exists(Path.Combine(factory.DataDirectory, "issuers", issuerId.ToString(), "quotes", $"{quoteId}.pdf")),
            "The finalization did not render the PDF.");
    }

    [Fact]
    public async Task Should_ReturnThePdfNamedAfterTheQuoteNumber_When_TheQuoteIsFinalized()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);
        await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);

        var response = await client.GetAsync($"/issuers/{issuerId}/quotes/{quoteId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("D-2026-0001.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("%PDF"u8.ToArray(), (await response.Content.ReadAsByteArrayAsync())[..4]);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_AskingForThePdfOfADraft()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);

        var response = await client.GetAsync($"/issuers/{issuerId}/quotes/{quoteId}/pdf");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_AnotherIssuerAsksForThePdf()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteAsync(client);
        await AddLineAsync(client, issuerId, quoteId, "Audit", 800m);
        await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);
        var other = await ConfigureIssuerAsync(client);

        var response = await client.GetAsync($"/issuers/{other}/quotes/{quoteId}/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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

    private sealed record FinalizedQuoteBody(Guid Id, string Status, string? Number, string? IssuedOn, string? ValidUntil, IReadOnlyList<MentionBody> Mentions)
    {
        public static IEqualityComparer<FinalizedQuoteBody?> Comparer { get; } = EqualityComparer<FinalizedQuoteBody?>.Create(
            (left, right) => (left?.Id, left?.Status, left?.Number, left?.IssuedOn, left?.ValidUntil) == (right?.Id, right?.Status, right?.Number, right?.IssuedOn, right?.ValidUntil)
                && (left?.Mentions ?? []).SequenceEqual(right?.Mentions ?? []));
    }

    private sealed record QuoteWithTotalsBody(IReadOnlyList<LineBody> Lines, TotalsBody Totals, IReadOnlyList<VatBreakdownBody> VatBreakdown);
}
