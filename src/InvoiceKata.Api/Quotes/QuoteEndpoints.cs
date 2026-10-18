using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Api.Quotes;

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder routes)
    {
        var quotes = routes.MapGroup("/issuers/{issuerId:guid}/quotes");

        quotes.MapPost("/", async (Guid issuerId, CreateQuoteRequest request, CreateQuote createQuote, CancellationToken cancellationToken) =>
        {
            var id = await createQuote.HandleAsync(new IssuerId(issuerId), new CustomerId(request.CustomerId), cancellationToken);
            return Results.Created($"/issuers/{issuerId}/quotes/{id.Value}", new { id = id.Value });
        });

        quotes.MapGet("/{quoteId:guid}", async (Guid issuerId, Guid quoteId, GetQuote getQuote, CancellationToken cancellationToken) =>
        {
            var quote = await getQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), cancellationToken);
            return Results.Ok(QuoteResponse.From(quote));
        });

        return routes;
    }
}

public sealed record CreateQuoteRequest(Guid CustomerId);

public sealed record QuoteResponse(Guid Id, Guid CustomerId, string Status, IReadOnlyList<object> Lines)
{
    public static QuoteResponse From(Quote quote) =>
        new(quote.Id.Value, quote.CustomerId.Value, quote.Status.ToString(), []);
}
