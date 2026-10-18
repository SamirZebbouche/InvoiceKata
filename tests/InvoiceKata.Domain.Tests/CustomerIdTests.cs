namespace InvoiceKata.Domain.Tests;

public sealed class CustomerIdTests
{
    [Fact]
    public void Should_BeARandomVersion4Guid_When_Created()
    {
        var id = CustomerId.New();

        Assert.Equal(4, id.Value.Version);
    }
}
