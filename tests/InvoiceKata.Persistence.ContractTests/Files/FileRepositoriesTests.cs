using InvoiceKata.Adapters.Persistence.Files;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
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

    public void Dispose() => _directory.Dispose();
}
