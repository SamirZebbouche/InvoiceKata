using InvoiceKata.Application.Invoices;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceKata.Application.Tests.Invoices;

public sealed class IssueInvoiceTests
{
    private readonly StoredQuotes _quotes = new();
    private readonly StoredInvoices _invoices = new();
    private readonly FrenchMentionTexts _texts = new();
    private readonly QuoteScope _scope = Scopes.Draft();

    [Fact]
    public async Task Should_IssueTheInvoiceTodayWithTheFirstNumberAndThePaymentMentions_When_TheQuoteIsFinalized()
    {
        var quote = await StoredFinalizedQuoteAsync(_scope.Quote);

        var id = await UseCase(at: "2026-11-30T10:00:00Z").HandleAsync(quote.IssuerId, quote.Id, IssueInvoice.DefaultPaymentDays, CancellationToken.None);

        var invoice = await _invoices.GetAsync(quote.IssuerId, id, CancellationToken.None);
        Assert.Equal((new InvoiceNumber(2026, 1), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30), quote.Id), (invoice.Number, invoice.IssuedOn, invoice.DueOn, invoice.QuoteId));
        LegalMention[] payment =
        [
            new(MentionCode.PaymentDueDate, _texts.InvoiceDueDate(new DateOnly(2026, 12, 30))),
            new(MentionCode.EarlyPaymentDiscount, _texts.NoEarlyPaymentDiscount()),
            new(MentionCode.LatePaymentPenalties, _texts.LatePaymentPenalties()),
            new(MentionCode.RecoveryIndemnity, _texts.RecoveryIndemnity()),
        ];
        Assert.All(payment, mention => Assert.Contains(mention, invoice.Mentions));
    }

    [Fact]
    public async Task Should_FollowTheGreatestNumberTaken_When_TheIssuerInvoicedBefore()
    {
        var earlier = await StoredFinalizedQuoteAsync(Quote.DraftFor(_scope.Issuer, _scope.Customer));
        await UseCase(at: "2026-11-02T10:00:00Z").HandleAsync(earlier.IssuerId, earlier.Id, IssueInvoice.DefaultPaymentDays, CancellationToken.None);
        var quote = await StoredFinalizedQuoteAsync(_scope.Quote);

        var id = await UseCase(at: "2026-11-30T10:00:00Z").HandleAsync(quote.IssuerId, quote.Id, IssueInvoice.DefaultPaymentDays, CancellationToken.None);

        Assert.Equal(new InvoiceNumber(2026, 2), (await _invoices.GetAsync(quote.IssuerId, id, CancellationToken.None)).Number);
    }

    // The invoice is dated in France: at 23:30 UTC on 31 December, it is already 1 January in Paris.
    [Fact]
    public async Task Should_DateTheInvoiceInParis_When_ItIsAlreadyTheNextDayThere()
    {
        var quote = await StoredFinalizedQuoteAsync(_scope.Quote);

        var id = await UseCase(at: "2026-12-31T23:30:00Z").HandleAsync(quote.IssuerId, quote.Id, IssueInvoice.DefaultPaymentDays, CancellationToken.None);

        var invoice = await _invoices.GetAsync(quote.IssuerId, id, CancellationToken.None);
        Assert.Equal((new DateOnly(2027, 1, 1), new InvoiceNumber(2027, 1)), (invoice.IssuedOn, invoice.Number));
    }

    // The use case's half of "one invoice per quote" (ADR-0010); the port holds the other half.
    [Fact]
    public async Task Should_RefuseASecondInvoice_When_TheQuoteAlreadyHasOne()
    {
        var quote = await StoredFinalizedQuoteAsync(_scope.Quote);
        var issue = UseCase(at: "2026-11-30T10:00:00Z");
        await issue.HandleAsync(quote.IssuerId, quote.Id, IssueInvoice.DefaultPaymentDays, CancellationToken.None);

        await Assert.ThrowsAsync<QuoteAlreadyInvoicedException>(() => issue.HandleAsync(quote.IssuerId, quote.Id, IssueInvoice.DefaultPaymentDays, CancellationToken.None));
        Assert.Single(_invoices.All);
    }

    // Between professionals, the payment term cannot exceed 60 days (ADR-0010).
    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public async Task Should_RefuseThePaymentTermAndIssueNothing_When_ItIsNotBetweenOneAndSixtyDays(int paymentDays)
    {
        var quote = await StoredFinalizedQuoteAsync(_scope.Quote);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            UseCase(at: "2026-11-30T10:00:00Z").HandleAsync(quote.IssuerId, quote.Id, paymentDays, CancellationToken.None));
        Assert.Empty(_invoices.All);
    }

    [Fact]
    public async Task Should_PublishInvoiceIssuedAfterTheAdd_When_TheInvoiceIsIssued()
    {
        var quote = await StoredFinalizedQuoteAsync(_scope.Quote);
        var heard = new List<(IInvoiceEvent Event, int Stored)>();
        var reaction = new ReadingReaction(quoteEvent => heard.Add((quoteEvent, _invoices.All.Count)));

        var id = await UseCase(at: "2026-11-30T10:00:00Z", reaction).HandleAsync(quote.IssuerId, quote.Id, IssueInvoice.DefaultPaymentDays, CancellationToken.None);

        Assert.Equal([((IInvoiceEvent)new InvoiceIssued(id, quote.IssuerId, new InvoiceNumber(2026, 1)), 1)], heard);
    }

    private IssueInvoice UseCase(string at, params IInvoiceReaction[] reactions) => new(
        _quotes, _invoices, _texts, new FixedClock(DateTimeOffset.Parse(at, System.Globalization.CultureInfo.InvariantCulture)),
        new InvoiceReactions(reactions, NullLogger<InvoiceReactions>.Instance));

    private async Task<Quote> StoredFinalizedQuoteAsync(Quote quote)
    {
        quote.AddLine(Scopes.Line(vatRate: 20m));
        var taken = await _quotes.TakenNumbersAsync(quote.IssuerId, CancellationToken.None);
        quote.Finalize(new QuoteNumbering(taken).Next(2026), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        await _quotes.AddAsync(quote, CancellationToken.None);
        return quote;
    }

    private sealed class ReadingReaction(Action<IInvoiceEvent> react) : IInvoiceReaction
    {
        public Task ReactAsync(IInvoiceEvent invoiceEvent, CancellationToken cancellationToken)
        {
            react(invoiceEvent);
            return Task.CompletedTask;
        }
    }
}
