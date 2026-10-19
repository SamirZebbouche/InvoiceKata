using System.ComponentModel.DataAnnotations;

using InvoiceKata.Api.Quotes;
using InvoiceKata.Application.Invoices;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Mvc;

namespace InvoiceKata.Api.Invoices;

[ApiController]
[Route("issuers/{issuerId:guid}/invoices")]
public sealed class InvoicesController(IssueInvoice issueInvoice, GetInvoice getInvoice, RenderInvoicePdf renderInvoicePdf) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IssueAsync(Guid issuerId, IssueInvoiceRequest request, CancellationToken cancellationToken)
    {
        var id = await issueInvoice.HandleAsync(
            new IssuerId(issuerId), new QuoteId(request.QuoteId), request.PaymentDays ?? IssueInvoice.DefaultPaymentDays, cancellationToken);
        var invoice = await getInvoice.HandleAsync(new IssuerId(issuerId), id, cancellationToken);
        return Created($"/issuers/{issuerId}/invoices/{id.Value}", InvoiceResponse.From(invoice));
    }

    [HttpGet("{invoiceId:guid}")]
    public async Task<InvoiceResponse> GetAsync(Guid issuerId, Guid invoiceId, CancellationToken cancellationToken) =>
        InvoiceResponse.From(await getInvoice.HandleAsync(new IssuerId(issuerId), new InvoiceId(invoiceId), cancellationToken));

    // The PDF rendered at the issue, or rendered now if that rendering failed; named after the invoice number.
    [HttpGet("{invoiceId:guid}/pdf")]
    public async Task<IActionResult> GetPdfAsync(Guid issuerId, Guid invoiceId, CancellationToken cancellationToken)
    {
        var pdf = await renderInvoicePdf.PdfOfAsync(new IssuerId(issuerId), new InvoiceId(invoiceId), cancellationToken);
        var invoice = await getInvoice.HandleAsync(new IssuerId(issuerId), new InvoiceId(invoiceId), cancellationToken);
        return File(pdf, "application/pdf", $"{invoice.Number}.pdf");
    }
}

// Between professionals, the payment term cannot exceed 60 days (ADR-0010).
public sealed record IssueInvoiceRequest(Guid QuoteId, [Range(1, IssueInvoice.MaximumPaymentDays)] int? PaymentDays = null);

public sealed record InvoiceResponse(
    Guid Id,
    string Number,
    DateOnly IssuedOn,
    DateOnly DueOn,
    Guid QuoteId,
    string QuoteNumber,
    Guid CustomerId,
    IReadOnlyList<QuoteLineResponse> Lines,
    TotalsResponse Totals,
    IReadOnlyList<VatBreakdownResponse> VatBreakdown,
    IReadOnlyList<MentionResponse> Mentions)
{
    public static InvoiceResponse From(Invoice invoice) => new(
        invoice.Id.Value,
        invoice.Number.ToString(),
        invoice.IssuedOn,
        invoice.DueOn,
        invoice.QuoteId.Value,
        invoice.QuoteNumber.ToString(),
        invoice.CustomerId.Value,
        [.. invoice.Lines.Select(QuoteLineResponse.From)],
        new TotalsResponse(invoice.TotalExcludingVat.Amount, invoice.TotalVat.Amount, invoice.TotalIncludingVat.Amount),
        [.. invoice.VatBreakdown.Select(rate => new VatBreakdownResponse(rate.Rate.Percent, rate.BaseExcludingVat.Amount, rate.Vat.Amount))],
        [.. invoice.Mentions.Select(mention => new MentionResponse(mention.Code.ToString(), mention.Text))]);
}
