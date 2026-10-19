namespace InvoiceKata.Domain.Tests;

public sealed class InvoiceIdTests
{
    [Fact]
    public void Should_BeARandomVersion4Guid_When_Created()
    {
        var id = InvoiceId.New();

        Assert.Equal(4, id.Value.Version);
    }
}
