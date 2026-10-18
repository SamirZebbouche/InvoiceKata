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

        quotes.MapPost("/{quoteId:guid}/lines", async (Guid issuerId, Guid quoteId, AddQuoteLineRequest request, AddQuoteLine addQuoteLine, GetQuote getQuote, CancellationToken cancellationToken) =>
        {
            var lineId = await addQuoteLine.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), request.ToNewLine(), cancellationToken);
            var quote = await getQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), cancellationToken);
            return Results.Created($"/issuers/{issuerId}/quotes/{quoteId}/lines/{lineId.Value}", QuoteResponse.From(quote));
        });

        quotes.MapDelete("/{quoteId:guid}/lines/{lineId:guid}", async (Guid issuerId, Guid quoteId, Guid lineId, RemoveQuoteLine removeQuoteLine, GetQuote getQuote, CancellationToken cancellationToken) =>
        {
            await removeQuoteLine.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), new QuoteLineId(lineId), cancellationToken);
            var quote = await getQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), cancellationToken);
            return Results.Ok(QuoteResponse.From(quote));
        });

        return routes;
    }
}

public sealed record CreateQuoteRequest(Guid CustomerId);

public sealed record AddQuoteLineRequest(string Designation, decimal Quantity, string Unit, decimal UnitPriceExcludingVat, decimal VatRate)
{
    public NewQuoteLine ToNewLine() => new()
    {
        Designation = Designation,
        Quantity = new Quantity(Quantity),
        Unit = Unit,
        UnitPriceExcludingVat = new Money(UnitPriceExcludingVat),
        VatRate = new VatRate(VatRate),
    };
}

public sealed record QuoteLineResponse(Guid Id, string Designation, decimal Quantity, string Unit, decimal UnitPriceExcludingVat, decimal VatRate, decimal TotalExcludingVat)
{
    public static QuoteLineResponse From(QuoteLine line) => new(
        line.Id.Value, line.Designation, line.Quantity.Value, line.Unit,
        line.UnitPriceExcludingVat.Amount, line.VatRate.Percent, line.TotalExcludingVat.Amount);
}

public sealed record TotalsResponse(decimal ExcludingVat, decimal Vat, decimal IncludingVat);

public sealed record VatBreakdownResponse(decimal Rate, decimal BaseExcludingVat, decimal Vat);

public sealed record QuoteResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    IReadOnlyList<QuoteLineResponse> Lines,
    TotalsResponse Totals,
    IReadOnlyList<VatBreakdownResponse> VatBreakdown)
{
    public static QuoteResponse From(Quote quote) => new(
        quote.Id.Value,
        quote.CustomerId.Value,
        quote.Status.ToString(),
        [.. quote.Lines.Select(QuoteLineResponse.From)],
        new TotalsResponse(quote.TotalExcludingVat.Amount, quote.TotalVat.Amount, quote.TotalIncludingVat.Amount),
        [.. quote.VatBreakdown.Select(rate => new VatBreakdownResponse(rate.Rate.Percent, rate.BaseExcludingVat.Amount, rate.Vat.Amount))]);
}
