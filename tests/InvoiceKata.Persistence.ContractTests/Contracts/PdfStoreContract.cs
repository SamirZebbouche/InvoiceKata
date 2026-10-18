using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Persistence.ContractTests.Contracts;

public abstract class PdfStoreContract
{
    private static readonly byte[] First = "%PDF-1.7 first"u8.ToArray();
    private static readonly byte[] Second = "%PDF-1.7 second"u8.ToArray();

    protected abstract IPdfStore CreateStore();

    [Fact]
    public async Task Should_GiveBackTheStoredPdf_When_ItWasSaved()
    {
        var store = CreateStore();
        var (issuerId, quoteId) = (IssuerId.New(), QuoteId.New());

        await store.SaveIfAbsentAsync(issuerId, quoteId, First, CancellationToken.None);

        Assert.Equal(First, await store.FindAsync(issuerId, quoteId, CancellationToken.None));
    }

    [Fact]
    public async Task Should_GiveNothing_When_NoPdfWasSaved()
    {
        Assert.Null(await CreateStore().FindAsync(IssuerId.New(), QuoteId.New(), CancellationToken.None));
    }

    // The PDF is a copy of a frozen document: the first one stays, so rendering again changes nothing.
    [Fact]
    public async Task Should_KeepTheFirstPdf_When_AnotherIsSavedForTheSameQuote()
    {
        var store = CreateStore();
        var (issuerId, quoteId) = (IssuerId.New(), QuoteId.New());
        await store.SaveIfAbsentAsync(issuerId, quoteId, First, CancellationToken.None);

        await store.SaveIfAbsentAsync(issuerId, quoteId, Second, CancellationToken.None);

        Assert.Equal(First, await store.FindAsync(issuerId, quoteId, CancellationToken.None));
    }

    [Fact]
    public async Task Should_GiveNothing_When_AnotherIssuerAsksForThePdf()
    {
        var store = CreateStore();
        var quoteId = QuoteId.New();
        await store.SaveIfAbsentAsync(IssuerId.New(), quoteId, First, CancellationToken.None);

        Assert.Null(await store.FindAsync(IssuerId.New(), quoteId, CancellationToken.None));
    }
}
