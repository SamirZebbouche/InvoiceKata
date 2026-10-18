using System.ComponentModel.DataAnnotations;

using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace InvoiceKata.Api.Quotes;

[ApiController]
[Route("issuers/{issuerId:guid}/quotes")]
public sealed class QuotesController(
    CreateQuote createQuote, GetQuote getQuote, AddQuoteLine addQuoteLine, RemoveQuoteLine removeQuoteLine, FinalizeQuote finalizeQuote)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(Guid issuerId, CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var id = await createQuote.HandleAsync(new IssuerId(issuerId), new CustomerId(request.CustomerId), cancellationToken);
        return Created($"/issuers/{issuerId}/quotes/{id.Value}", new { id = id.Value });
    }

    [HttpGet("{quoteId:guid}")]
    public async Task<QuoteResponse> GetAsync(Guid issuerId, Guid quoteId, CancellationToken cancellationToken) =>
        QuoteResponse.From(await getQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), cancellationToken));

    [HttpPost("{quoteId:guid}/lines")]
    public async Task<IActionResult> AddLineAsync(Guid issuerId, Guid quoteId, AddQuoteLineRequest request, CancellationToken cancellationToken)
    {
        var lineId = await addQuoteLine.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), request.ToNewLine(), cancellationToken);
        var quote = await getQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), cancellationToken);
        return Created($"/issuers/{issuerId}/quotes/{quoteId}/lines/{lineId.Value}", QuoteResponse.From(quote));
    }

    [HttpDelete("{quoteId:guid}/lines/{lineId:guid}")]
    public async Task<QuoteResponse> RemoveLineAsync(Guid issuerId, Guid quoteId, Guid lineId, CancellationToken cancellationToken)
    {
        await removeQuoteLine.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), new QuoteLineId(lineId), cancellationToken);
        return QuoteResponse.From(await getQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), cancellationToken));
    }

    // The body is optional: without it, the quote is valid for the default duration.
    [HttpPost("{quoteId:guid}/finalization")]
    public async Task<QuoteResponse> FinalizeAsync(
        Guid issuerId, Guid quoteId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] FinalizeQuoteRequest? request, CancellationToken cancellationToken)
    {
        var validityDays = request?.ValidityDays ?? FinalizeQuote.DefaultValidityDays;
        await finalizeQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), validityDays, cancellationToken);
        return QuoteResponse.From(await getQuote.HandleAsync(new IssuerId(issuerId), new QuoteId(quoteId), cancellationToken));
    }
}

public sealed record FinalizeQuoteRequest([Range(1, 365)] int? ValidityDays);

public sealed record CreateQuoteRequest(Guid CustomerId);

public sealed record AddQuoteLineRequest(string Designation, decimal Quantity, string Unit, decimal UnitPriceExcludingVat, decimal VatRate, decimal? DiscountRate = null)
{
    public NewQuoteLine ToNewLine() => new()
    {
        Designation = Designation,
        Quantity = new Quantity(Quantity),
        Unit = Unit,
        UnitPriceExcludingVat = new Money(UnitPriceExcludingVat),
        VatRate = new VatRate(VatRate),
        Discount = DiscountRate is { } percent ? new DiscountRate(percent) : Domain.DiscountRate.None,
    };
}

public sealed record QuoteLineResponse(Guid Id, string Designation, decimal Quantity, string Unit, decimal UnitPriceExcludingVat, decimal DiscountRate, decimal NetUnitPriceExcludingVat, decimal VatRate, decimal TotalExcludingVat)
{
    public static QuoteLineResponse From(QuoteLine line) => new(
        line.Id.Value, line.Designation, line.Quantity.Value, line.Unit,
        line.UnitPriceExcludingVat.Amount, line.Discount.Percent, line.NetUnitPriceExcludingVat.Amount, line.VatRate.Percent, line.TotalExcludingVat.Amount);
}

public sealed record TotalsResponse(decimal ExcludingVat, decimal Vat, decimal IncludingVat);

public sealed record VatBreakdownResponse(decimal Rate, decimal BaseExcludingVat, decimal Vat);

public sealed record MentionResponse(string Code, string Text);

public sealed record QuoteResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    string? Number,
    DateOnly? IssuedOn,
    DateOnly? ValidUntil,
    IReadOnlyList<QuoteLineResponse> Lines,
    TotalsResponse Totals,
    IReadOnlyList<VatBreakdownResponse> VatBreakdown,
    IReadOnlyList<MentionResponse> Mentions)
{
    public static QuoteResponse From(Quote quote) => new(
        quote.Id.Value,
        quote.CustomerId.Value,
        quote.Status.ToString(),
        quote.Number?.ToString(),
        quote.IssuedOn,
        quote.ValidUntil,
        [.. quote.Lines.Select(QuoteLineResponse.From)],
        new TotalsResponse(quote.TotalExcludingVat.Amount, quote.TotalVat.Amount, quote.TotalIncludingVat.Amount),
        [.. quote.VatBreakdown.Select(rate => new VatBreakdownResponse(rate.Rate.Percent, rate.BaseExcludingVat.Amount, rate.Vat.Amount))],
        [.. quote.Mentions.Select(mention => new MentionResponse(mention.Code.ToString(), mention.Text))]);
}
