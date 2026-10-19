using InvoiceKata.Adapters.Persistence.Files;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Invoices;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Persistence.ContractTests.Contracts;

namespace InvoiceKata.Persistence.ContractTests.Files;

// A fresh directory per test (xUnit builds one instance per test), deleted afterwards.
public sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("invoicekata-").FullName;

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

public sealed class FileIssuerRepositoryTests : IssuerRepositoryContract, IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    protected override IIssuerRepository CreateRepository() => new FileIssuerRepository(new JsonFileStore(_directory.Path));

    public void Dispose() => _directory.Dispose();
}

public sealed class FileCustomerRepositoryTests : CustomerRepositoryContract, IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    protected override ICustomerRepository CreateRepository() => new FileCustomerRepository(new JsonFileStore(_directory.Path));

    public void Dispose() => _directory.Dispose();
}

public sealed class FileQuoteRepositoryTests : QuoteRepositoryContract, IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    protected override IQuoteRepository CreateRepository() => new FileQuoteRepository(new JsonFileStore(_directory.Path));

    // A crash during an atomic write can leave a temporary file next to the quotes: it is not a quote.
    [Fact]
    public async Task Should_IgnoreALeftoverTemporaryFile_When_ReadingTheTakenNumbers()
    {
        var issuerId = InvoiceKata.Domain.IssuerId.New();
        var quotes = System.IO.Path.Combine(_directory.Path, "issuers", issuerId.Value.ToString(), "quotes");
        Directory.CreateDirectory(quotes);
        await File.WriteAllTextAsync(System.IO.Path.Combine(quotes, $"{Guid.NewGuid()}.json.{Guid.NewGuid():N}.tmp"), "{ \"version\": 2, \"half");

        var taken = await CreateRepository().TakenNumbersAsync(issuerId, CancellationToken.None);

        Assert.Empty(taken);
    }

    public void Dispose() => _directory.Dispose();
}

public sealed class FilePdfStoreTests : PdfStoreContract, IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    protected override IPdfStore CreateStore() => new FilePdfStore(_directory.Path);

    [Fact]
    public async Task Should_LeaveOnlyThePdf_When_ASecondOneIsRefused()
    {
        var store = CreateStore();
        var (issuerId, quoteId) = (InvoiceKata.Domain.IssuerId.New(), InvoiceKata.Domain.QuoteId.New());
        var document = DocumentReference.Of(quoteId);
        await store.SaveIfAbsentAsync(issuerId, document, "%PDF first"u8.ToArray(), CancellationToken.None);

        await store.SaveIfAbsentAsync(issuerId, document, "%PDF second"u8.ToArray(), CancellationToken.None);

        var folder = System.IO.Path.Combine(_directory.Path, "issuers", issuerId.Value.ToString(), "quotes");
        Assert.Equal([$"{quoteId.Value}.pdf"], Directory.EnumerateFiles(folder).Select(System.IO.Path.GetFileName));
    }

    public void Dispose() => _directory.Dispose();
}

public sealed class FileInvoiceRepositoryTests : InvoiceRepositoryContract, IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    protected override IInvoiceRepository CreateRepository() => new FileInvoiceRepository(new JsonFileStore(_directory.Path));

    public void Dispose() => _directory.Dispose();
}
