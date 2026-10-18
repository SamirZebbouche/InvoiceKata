namespace InvoiceKata.Domain.Tests;

public sealed class IssuerIdTests
{
    [Fact]
    public void Should_BeARandomVersion4Guid_When_Created()
    {
        var id = IssuerId.New();

        Assert.Equal(4, id.Value.Version);
    }

    [Fact]
    public void Should_DifferFromThePreviousOne_When_CreatedTwice()
    {
        var first = IssuerId.New();

        var second = IssuerId.New();

        Assert.NotEqual(first, second);
    }
}
