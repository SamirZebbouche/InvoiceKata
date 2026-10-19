using InvoiceKata.Application.Invoices;
using InvoiceKata.Domain;

namespace InvoiceKata.Persistence.ContractTests.Contracts;

public abstract class InvoiceRepositoryContract
{
    protected abstract IInvoiceRepository CreateRepository();

    [Fact]
    public async Task Should_GiveBackTheSameInvoice_When_ItWasAdded()
    {
        var repository = CreateRepository();
        var invoice = Issued(Samples.Issuer(), new InvoiceNumber(2026, 1));

        await repository.AddAsync(invoice, CancellationToken.None);
        var found = await repository.GetAsync(invoice.IssuerId, invoice.Id, CancellationToken.None);

        Assert.Equivalent(invoice.ToState(), found.ToState(), strict: true);
        Assert.Empty(found.TakeEvents());
    }

    [Fact]
    public async Task Should_RefuseToGiveAnInvoice_When_AnotherIssuerAsksForIt()
    {
        var repository = CreateRepository();
        var invoice = Issued(Samples.Issuer(), new InvoiceNumber(2026, 1));
        await repository.AddAsync(invoice, CancellationToken.None);

        await Assert.ThrowsAsync<InvoiceNotFoundException>(() => repository.GetAsync(IssuerId.New(), invoice.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Should_GiveTheNumbersOfThisIssuersInvoicesOnly_When_AskedForTheTakenNumbers()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        await repository.AddAsync(Issued(issuer, new InvoiceNumber(2026, 1)), CancellationToken.None);
        await repository.AddAsync(Issued(issuer, new InvoiceNumber(2026, 2)), CancellationToken.None);
        await repository.AddAsync(Issued(Samples.Issuer(), new InvoiceNumber(2026, 3)), CancellationToken.None);

        var taken = await repository.TakenNumbersAsync(issuer.Id, CancellationToken.None);

        Assert.Equal([new InvoiceNumber(2026, 1), new InvoiceNumber(2026, 2)], taken.OrderBy(number => number.Sequence));
    }

    // The port's half of the guarantees (ADR-0010): a concurrent issue could have computed the same number.
    [Fact]
    public async Task Should_RefuseTheInvoice_When_AnotherInvoiceOfTheIssuerHasItsNumber()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        await repository.AddAsync(Issued(issuer, new InvoiceNumber(2026, 1)), CancellationToken.None);
        var duplicate = Issued(issuer, new InvoiceNumber(2026, 1));

        await Assert.ThrowsAsync<InvoiceNumberAlreadyTakenException>(() => repository.AddAsync(duplicate, CancellationToken.None));
        await Assert.ThrowsAsync<InvoiceNotFoundException>(() => repository.GetAsync(issuer.Id, duplicate.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Should_RefuseASecondInvoice_When_TheQuoteIsAlreadyInvoiced()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        var quote = FinalizedQuote(issuer);
        await repository.AddAsync(Issue(quote, new InvoiceNumber(2026, 1)), CancellationToken.None);

        await Assert.ThrowsAsync<QuoteAlreadyInvoicedException>(() => repository.AddAsync(Issue(quote, new InvoiceNumber(2026, 2)), CancellationToken.None));
    }

    [Fact]
    public async Task Should_FindTheInvoiceOfAQuote_When_TheQuoteIsInvoiced()
    {
        var repository = CreateRepository();
        var issuer = Samples.Issuer();
        var quote = FinalizedQuote(issuer);
        var invoice = Issue(quote, new InvoiceNumber(2026, 1));
        await repository.AddAsync(invoice, CancellationToken.None);

        Assert.Equal(invoice.Id, (await repository.FindByQuoteAsync(issuer.Id, quote.Id, CancellationToken.None))?.Id);
        Assert.Null(await repository.FindByQuoteAsync(issuer.Id, QuoteId.New(), CancellationToken.None));
    }

    // An issued invoice is never written over (ADR-0006), even with itself.
    [Fact]
    public async Task Should_RefuseToWriteOverAnInvoice_When_ItIsAddedAgain()
    {
        var repository = CreateRepository();
        var invoice = Issued(Samples.Issuer(), new InvoiceNumber(2026, 1));
        await repository.AddAsync(invoice, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(invoice, CancellationToken.None));
    }

    private static Invoice Issued(Issuer issuer, InvoiceNumber number) => Issue(FinalizedQuote(issuer), number);

    private static Invoice Issue(Quote quote, InvoiceNumber number) => Invoice.IssueFor(
        quote, number, new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30), new PaymentMentionTexts(
            "Échéance : 30/12/2026.", "Escompte pour paiement anticipé : néant.",
            "Pénalités de retard : trois fois le taux d'intérêt légal.", "Indemnité forfaitaire pour frais de recouvrement : 40 €."));

    private static Quote FinalizedQuote(Issuer issuer)
    {
        var quote = Quote.DraftFor(issuer, Samples.ProfessionalOf(issuer));
        quote.AddLine(Samples.Line("Faïence", 33.33m, 10m, discount: 10m));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), "Devis valable 30 jours, jusqu'au 17/11/2026.");
        return quote;
    }
}
