namespace InvoiceKata.Domain.Tests;

public sealed class QuoteIdTests
{
    [Fact]
    public void Should_BeARandomVersion4Guid_When_Created()
    {
        var id = QuoteId.New();

        Assert.Equal(4, id.Value.Version);
    }
}
