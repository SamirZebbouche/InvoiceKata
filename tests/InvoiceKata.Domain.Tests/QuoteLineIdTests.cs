namespace InvoiceKata.Domain.Tests;

public sealed class QuoteLineIdTests
{
    [Fact]
    public void Should_BeARandomVersion4Guid_When_Created()
    {
        var id = QuoteLineId.New();

        Assert.Equal(4, id.Value.Version);
    }
}
