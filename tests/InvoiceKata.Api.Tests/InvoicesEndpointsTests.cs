using System.Net;
using System.Net.Http.Json;

namespace InvoiceKata.Api.Tests;

public sealed class InvoicesEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Should_ReturnTheInvoiceAtItsLocation_When_IssuingTheInvoiceOfAFinalizedQuote()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteAsync(client);

        var issued = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });

        Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
        var invoice = await issued.Content.ReadFromJsonAsync<InvoiceBody>();
        Assert.Equal(("F-2026-0001", "2026-10-18", "2026-11-17", quoteId, "D-2026-0001"), (invoice!.Number, invoice.IssuedOn, invoice.DueOn, invoice.QuoteId, invoice.QuoteNumber));
        Assert.Equal($"/issuers/{issuerId}/invoices/{invoice.Id}", issued.Headers.Location!.OriginalString);
        Assert.Equal(invoice.Id, (await client.GetFromJsonAsync<InvoiceBody>(issued.Headers.Location))!.Id);
    }

    [Fact]
    public async Task Should_EndThePaymentTermAfterTheDaysAsked_When_IssuingWithAPaymentTerm()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteAsync(client);

        var issued = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId, paymentDays = 45 });

        Assert.Equal("2026-12-02", (await issued.Content.ReadFromJsonAsync<InvoiceBody>())!.DueOn);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public async Task Should_ReturnBadRequest_When_ThePaymentTermIsNotBetweenOneAndSixtyDays(int paymentDays)
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteAsync(client);

        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId, paymentDays });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_TheQuoteAlreadyHasItsInvoice()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteAsync(client);
        await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });

        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_TheQuoteIsADraft()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await DraftQuoteWithALineAsync(client);

        var response = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_AnotherIssuerReadsTheInvoice()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteAsync(client);
        var issued = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });
        var invoiceId = (await issued.Content.ReadFromJsonAsync<InvoiceBody>())!.Id;
        var other = await ConfigureIssuerAsync(client);

        var response = await client.GetAsync($"/issuers/{other}/invoices/{invoiceId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // The PDF is rendered as a reaction to the issue, in the same request (ADR-0010).
    [Fact]
    public async Task Should_StoreThePdfNextToTheInvoice_When_Issuing()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteAsync(client);

        var issued = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });

        var invoiceId = (await issued.Content.ReadFromJsonAsync<InvoiceBody>())!.Id;
        Assert.True(File.Exists(Path.Combine(factory.DataDirectory, "issuers", issuerId.ToString(), "invoices", $"{invoiceId}.pdf")),
            "The issue did not render the PDF.");
    }

    [Fact]
    public async Task Should_ReturnThePdfNamedAfterTheInvoiceNumber_When_TheInvoiceIsIssued()
    {
        using var typst = ApiFactory.WithTypst();
        var client = typst.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteAsync(client);
        var issued = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });
        var invoiceId = (await issued.Content.ReadFromJsonAsync<InvoiceBody>())!.Id;

        var response = await client.GetAsync($"/issuers/{issuerId}/invoices/{invoiceId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("F-2026-0001.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("%PDF-1."u8.ToArray(), (await response.Content.ReadAsByteArrayAsync())[..7]);
    }

    private static async Task<(Guid IssuerId, Guid QuoteId)> FinalizedQuoteAsync(HttpClient client)
    {
        var (issuerId, quoteId) = await DraftQuoteWithALineAsync(client);
        await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);
        return (issuerId, quoteId);
    }

    private static async Task<(Guid IssuerId, Guid QuoteId)> DraftQuoteWithALineAsync(HttpClient client)
    {
        var issuerId = await ConfigureIssuerAsync(client);
        var customer = await client.PostAsJsonAsync($"/issuers/{issuerId}/customers", new
        {
            name = "Les Toits Rouges SARL",
            siren = "834567890",
            vatNumber = "FR45834567890",
            address = new { street = "4 place du Marché", postalCode = "33000", city = "Bordeaux" },
        });
        var quoteId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes", new { customerId = IdFrom(customer) }));
        await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", new
        {
            designation = "Audit",
            quantity = 2m,
            unit = "jour",
            unitPriceExcludingVat = 800m,
            vatRate = 20m,
        });
        return (issuerId, quoteId);
    }

    private static async Task<Guid> ConfigureIssuerAsync(HttpClient client) => IdFrom(await client.PostAsJsonAsync("/issuers", new
    {
        name = "Batifolio",
        legalForm = "SAS",
        shareCapital = 20000m,
        siren = "912345678",
        vatNumber = "FR12912345678",
        address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
    }));

    private static Guid IdFrom(HttpResponseMessage created) => Guid.Parse(created.Headers.Location!.OriginalString.Split('/')[^1]);

    private sealed record InvoiceBody(Guid Id, string Number, string IssuedOn, string DueOn, Guid QuoteId, string QuoteNumber);
}
