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
        var (issuerId, quoteId) = (IssuerId.New(), DocumentReference.Of(QuoteId.New()));

        await store.SaveIfAbsentAsync(issuerId, quoteId, First, CancellationToken.None);

        Assert.Equal(First, await store.FindAsync(issuerId, quoteId, CancellationToken.None));
    }

    [Fact]
    public async Task Should_GiveNothing_When_NoPdfWasSaved()
    {
        Assert.Null(await CreateStore().FindAsync(IssuerId.New(), DocumentReference.Of(QuoteId.New()), CancellationToken.None));
    }

    // The PDF is a copy of a frozen document: the first one stays, so rendering again changes nothing.
    [Fact]
    public async Task Should_KeepTheFirstPdf_When_AnotherIsSavedForTheSameQuote()
    {
        var store = CreateStore();
        var (issuerId, quoteId) = (IssuerId.New(), DocumentReference.Of(QuoteId.New()));
        await store.SaveIfAbsentAsync(issuerId, quoteId, First, CancellationToken.None);

        await store.SaveIfAbsentAsync(issuerId, quoteId, Second, CancellationToken.None);

        Assert.Equal(First, await store.FindAsync(issuerId, quoteId, CancellationToken.None));
    }

    // Every kind of document keeps its own PDF, even with the same identifier.
    [Fact]
    public async Task Should_KeepOnePdfPerKindOfDocument_When_TheirIdentifiersAreTheSame()
    {
        var store = CreateStore();
        var (issuerId, id) = (IssuerId.New(), Guid.NewGuid());
        var kinds = Enum.GetValues<DocumentKind>();
        foreach (var kind in kinds)
        {
            await store.SaveIfAbsentAsync(issuerId, new DocumentReference(kind, id), System.Text.Encoding.UTF8.GetBytes($"%PDF {kind}"), CancellationToken.None);
        }

        foreach (var kind in kinds)
        {
            Assert.Equal(System.Text.Encoding.UTF8.GetBytes($"%PDF {kind}"), await store.FindAsync(issuerId, new DocumentReference(kind, id), CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_GiveNothing_When_AnotherIssuerAsksForThePdf()
    {
        var store = CreateStore();
        var quoteId = DocumentReference.Of(QuoteId.New());
        await store.SaveIfAbsentAsync(IssuerId.New(), quoteId, First, CancellationToken.None);

        Assert.Null(await store.FindAsync(IssuerId.New(), quoteId, CancellationToken.None));
    }
}
