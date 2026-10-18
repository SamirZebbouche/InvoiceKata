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

    public void Dispose() => _directory.Dispose();

    private void Install(string golden, string relativePath)
    {
        var target = Path.Combine(_directory.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Files", "Golden", golden), target);
    }
}
