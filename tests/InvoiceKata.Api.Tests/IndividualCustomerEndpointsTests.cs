using System.Net;
using System.Net.Http.Json;

namespace InvoiceKata.Api.Tests;

// An individual customer through the API (ADR-0012): quoted and invoiced, without the 40 € indemnity.
public sealed class IndividualCustomerEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Should_LeaveTheRecoveryIndemnityOut_When_InvoicingAnIndividual()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteForAnIndividualAsync(client);

        var issued = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });

        var invoice = await issued.Content.ReadFromJsonAsync<InvoiceBody>();
        Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
        Assert.DoesNotContain(invoice!.Mentions, mention => mention.Code == "RecoveryIndemnity");
        Assert.Contains(invoice.Mentions, mention => mention.Code == "LatePaymentPenalties" && mention.Text.Contains("1231-6", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_ServeThePdfOfTheInvoice_When_TheCustomerIsAnIndividual()
    {
        var client = factory.CreateClient();
        var (issuerId, quoteId) = await FinalizedQuoteForAnIndividualAsync(client);
        var issued = await client.PostAsJsonAsync($"/issuers/{issuerId}/invoices", new { quoteId });

        var response = await client.GetAsync($"{issued.Headers.Location}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<(Guid IssuerId, Guid QuoteId)> FinalizedQuoteForAnIndividualAsync(HttpClient client)
    {
        var issuerId = IdFrom(await client.PostAsJsonAsync("/issuers", new
        {
            name = "Batifolio",
            legalForm = "SAS",
            shareCapital = 20000m,
            siren = "912345678",
            vatNumber = "FR12912345678",
            address = new { street = "12 rue des Artisans", postalCode = "47000", city = "Agen" },
        }));
        var customerId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/customers", new
        {
            kind = "Individual",
            name = "Jeanne Martin",
            address = new { street = "3 rue des Lilas", postalCode = "33000", city = "Bordeaux" },
        }));
        var quoteId = IdFrom(await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes", new { customerId }));
        await client.PostAsJsonAsync($"/issuers/{issuerId}/quotes/{quoteId}/lines", new
        {
            designation = "Pose de faïence",
            quantity = 1m,
            unit = "forfait",
            unitPriceExcludingVat = 500m,
            vatRate = 10m,
        });
        await client.PostAsync($"/issuers/{issuerId}/quotes/{quoteId}/finalization", content: null);
        return (issuerId, quoteId);
    }

    private static Guid IdFrom(HttpResponseMessage created) => Guid.Parse(created.Headers.Location!.OriginalString.Split('/')[^1]);

    private sealed record MentionBody(string Code, string Text);

    private sealed record InvoiceBody(IReadOnlyList<MentionBody> Mentions);
}
