using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Persistence.ContractTests.Contracts;

namespace InvoiceKata.Persistence.ContractTests.InMemory;

public sealed class InMemoryIssuerRepositoryTests : IssuerRepositoryContract
{
    protected override IIssuerRepository CreateRepository() => new InMemoryIssuerRepository();
}

public sealed class InMemoryCustomerRepositoryTests : CustomerRepositoryContract
{
    protected override ICustomerRepository CreateRepository() => new InMemoryCustomerRepository();
}

public sealed class InMemoryQuoteRepositoryTests : QuoteRepositoryContract
{
    protected override IQuoteRepository CreateRepository() => new InMemoryQuoteRepository();
}

public sealed class InMemoryPdfStoreTests : PdfStoreContract
{
    protected override IPdfStore CreateStore() => new InMemoryPdfStore();
}
