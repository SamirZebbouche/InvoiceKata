using InvoiceKata.Adapters.Persistence.Files;
using InvoiceKata.Domain;

namespace InvoiceKata.Persistence.ContractTests.Files;

// Version 1 files, frozen. If the shape of a state changes, these fail: write a version-to-version
// converter, read on load (ADR-0009), instead of changing these files.
public sealed class GoldenFilesTests : IDisposable
{
    private static readonly Guid IssuerId = Guid.Parse("6f9a3c2e-1b4d-4e8a-9c7f-2d5e8a1b3c4d");
    private static readonly Guid CustomerId = Guid.Parse("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");
    private static readonly Guid QuoteId = Guid.Parse("0c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f");
    private static readonly Guid LineId = Guid.Parse("f1e2d3c4-b5a6-4978-8a6b-5c4d3e2f1a0b");
    private static readonly Guid InvoiceId = Guid.Parse("9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d");

    private readonly TemporaryDirectory _directory = new();

    [Fact]
    public async Task Should_ReadAVersion1Issuer()
    {
        Install("issuer.v1.json", $"issuers/{IssuerId}/issuer.json");

        var issuer = await new FileIssuerRepository(new JsonFileStore(_directory.Path)).GetAsync(new IssuerId(IssuerId), CancellationToken.None);

        Assert.Equal(("Batifolio", LegalForm.Sas, 20000m, new Siren("912345678")), (issuer.Name, issuer.LegalForm, issuer.ShareCapital, issuer.Siren));
        Assert.Equal("Agen", issuer.Address.City);
    }

    [Fact]
    public async Task Should_ReadAVersion1Customer()
    {
        Install("customer.v1.json", $"issuers/{IssuerId}/customers/{CustomerId}.json");

        var customer = await new FileCustomerRepository(new JsonFileStore(_directory.Path))
            .GetAsync(new IssuerId(IssuerId), new CustomerId(CustomerId), CancellationToken.None);

        Assert.Equal((CustomerKind.Professional, "Les Toits Rouges SARL", new VatNumber("FR45834567890")), (customer.Kind, customer.Name, customer.VatNumber));
    }

    [Fact]
    public async Task Should_ReadAVersion1Quote()
    {
        Install("quote.v1.json", $"issuers/{IssuerId}/quotes/{QuoteId}.json");

        var quote = await new FileQuoteRepository(new JsonFileStore(_directory.Path))
            .GetAsync(new IssuerId(IssuerId), new QuoteId(QuoteId), CancellationToken.None);

        var line = Assert.Single(quote.Lines);
        Assert.Equal((new QuoteLineId(LineId), "Faïence", new Money(29.997m), new Money(561.54m)), (line.Id, line.Designation, line.NetUnitPriceExcludingVat, line.TotalExcludingVat));
        Assert.Equal([new LegalMention(MentionCode.ReducedVatRate, "Taux de TVA réduit de 10 %.")], quote.Mentions);
    }

    // The version 2 issuer: an individual entrepreneur in VAT franchise, with no company field (ADR-0011).
    [Fact]
    public async Task Should_ReadAVersion2IndividualEntrepreneur()
    {
        Install("issuer.v2.json", $"issuers/{IssuerId}/issuer.json");

        var issuer = await new FileIssuerRepository(new JsonFileStore(_directory.Path)).GetAsync(new IssuerId(IssuerId), CancellationToken.None);

        Assert.Equal((IssuerKind.IndividualEntrepreneur, VatRegime.Franchise, "Jean Dupont", (VatNumber?)null), (issuer.Kind, issuer.VatRegime, issuer.Name, issuer.VatNumber));
    }

    // Writing produces the version 2 format, byte for byte apart from line endings: readable, indented JSON.
    [Fact]
    public async Task Should_WriteAnIssuerInTheVersion2Format()
    {
        var store = new JsonFileStore(_directory.Path);
        var issuer = Issuer.FromState(new IssuerState(
            2, IssuerId, IssuerKind.IndividualEntrepreneur, VatRegime.Franchise, "Jean Dupont", null, null, "853123456", null,
            new Address { Street = "8 chemin des Vignes", PostalCode = "47000", City = "Agen" }));

        await new FileIssuerRepository(store).AddAsync(issuer, CancellationToken.None);

        var written = await File.ReadAllTextAsync(Path.Combine(_directory.Path, "issuers", IssuerId.ToString(), "issuer.json"));
        var golden = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Files", "Golden", "issuer.v2.json"));
        Assert.Equal(Normalized(golden), Normalized(written));
    }

    [Fact]
    public async Task Should_ReadAVersion2FinalizedQuoteWithItsAmountsAsIssued()
    {
        Install("quote.v2.json", $"issuers/{IssuerId}/quotes/{QuoteId}.json");

        var quote = await new FileQuoteRepository(new JsonFileStore(_directory.Path))
            .GetAsync(new IssuerId(IssuerId), new QuoteId(QuoteId), CancellationToken.None);

        Assert.Equal((QuoteStatus.Finalized, (QuoteNumber?)new QuoteNumber(2026, 42), (DateOnly?)new DateOnly(2026, 10, 18), (DateOnly?)new DateOnly(2026, 11, 17)),
            (quote.Status, quote.Number, quote.IssuedOn, quote.ValidUntil));
        Assert.Equal((new Money(561.54m), new Money(56.15m), new Money(617.69m)), (quote.TotalExcludingVat, quote.TotalVat, quote.TotalIncludingVat));
        Assert.Contains(new LegalMention(MentionCode.QuoteValidity, "Devis valable 30 jours, jusqu'au 17/11/2026."), quote.Mentions);
    }

    // A finalized quote is written in the version 2 format, its amounts included, byte for byte apart from line endings.
    [Fact]
    public async Task Should_WriteAFinalizedQuoteInTheVersion2Format()
    {
        var golden = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Files", "Golden", "quote.v2.json"));
        var quote = Quote.FromState(FinalizedQuoteState());

        await new FileQuoteRepository(new JsonFileStore(_directory.Path)).AddAsync(quote, CancellationToken.None);

        var written = await File.ReadAllTextAsync(Path.Combine(_directory.Path, "issuers", IssuerId.ToString(), "quotes", $"{QuoteId}.json"));
        Assert.Equal(Normalized(golden), Normalized(written));
    }

    [Fact]
    public async Task Should_ReadAVersion1InvoiceAsIssued()
    {
        Install("invoice.v1.json", $"issuers/{IssuerId}/invoices/{InvoiceId}.json");

        var invoice = await new FileInvoiceRepository(new JsonFileStore(_directory.Path))
            .GetAsync(new IssuerId(IssuerId), new InvoiceId(InvoiceId), CancellationToken.None);

        Assert.Equal((new InvoiceNumber(2026, 107), new QuoteNumber(2026, 42), new DateOnly(2026, 12, 30)), (invoice.Number, invoice.QuoteNumber, invoice.DueOn));
        Assert.Equal((new Money(561.54m), new Money(56.15m), new Money(617.69m)), (invoice.TotalExcludingVat, invoice.TotalVat, invoice.TotalIncludingVat));
        Assert.Contains(new LegalMention(MentionCode.RecoveryIndemnity, "Indemnité forfaitaire pour frais de recouvrement : 40 €."), invoice.Mentions);
    }

    // An invoice is written in the version 1 format, its amounts included, byte for byte apart from line endings.
    [Fact]
    public async Task Should_WriteAnInvoiceInTheVersion1Format()
    {
        var golden = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Files", "Golden", "invoice.v1.json"));
        var invoice = Invoice.FromState(new InvoiceState(
            1, InvoiceId, IssuerId, CustomerId, QuoteId, "D-2026-0042", new DateOnly(2026, 10, 18),
            "F-2026-0107", new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30),
            [new QuoteLineState(LineId, "Faïence", 18.72m, "m²", 33.33m, 10m, 10m)],
            561.54m, [new VatBreakdownState(10m, 561.54m, 56.15m)], 56.15m, 617.69m,
            [
                new LegalMention(MentionCode.ReducedVatRate, "Taux de TVA réduit de 10 %."),
                new LegalMention(MentionCode.PaymentDueDate, "Échéance : 30/12/2026."),
                new LegalMention(MentionCode.EarlyPaymentDiscount, "Escompte pour paiement anticipé : néant."),
                new LegalMention(MentionCode.LatePaymentPenalties, "Pénalités de retard : trois fois le taux d'intérêt légal."),
                new LegalMention(MentionCode.RecoveryIndemnity, "Indemnité forfaitaire pour frais de recouvrement : 40 €."),
            ]));

        await new FileInvoiceRepository(new JsonFileStore(_directory.Path)).AddAsync(invoice, CancellationToken.None);

        var written = await File.ReadAllTextAsync(Path.Combine(_directory.Path, "issuers", IssuerId.ToString(), "invoices", $"{InvoiceId}.json"));
        Assert.Equal(Normalized(golden), Normalized(written));
    }

    public void Dispose() => _directory.Dispose();

    private static QuoteState FinalizedQuoteState() => new(
        2, QuoteId, IssuerId, CustomerId, QuoteStatus.Finalized,
        [new QuoteLineState(LineId, "Faïence", 18.72m, "m²", 33.33m, 10m, 10m)],
        [
            new LegalMention(MentionCode.ReducedVatRate, "Taux de TVA réduit de 10 %."),
            new LegalMention(MentionCode.QuoteValidity, "Devis valable 30 jours, jusqu'au 17/11/2026."),
        ],
        new QuoteFinalizationState(
            "D-2026-0042", new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17),
            561.54m, [new VatBreakdownState(10m, 561.54m, 56.15m)], 56.15m, 617.69m));

    private static string Normalized(string json) => json.ReplaceLineEndings("\n").Trim();

    private void Install(string golden, string relativePath)
    {
        var target = Path.Combine(_directory.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Files", "Golden", golden), target);
    }
}
