namespace InvoiceKata.Domain.Tests;

public sealed class InvoiceNumberingTests
{
    [Fact]
    public void Should_StartAtOne_When_NoNumberIsTaken()
    {
        var numbering = new InvoiceNumbering([]);

        Assert.Equal(new InvoiceNumber(2026, 1), numbering.Next(2026));
    }

    // Never a number already taken, and never one that fills a gap: the next follows the greatest.
    [Fact]
    public void Should_FollowTheGreatestNumberOfTheYear_When_SomeAreTaken()
    {
        var numbering = new InvoiceNumbering([new InvoiceNumber(2026, 5), new InvoiceNumber(2026, 1), new InvoiceNumber(2026, 2)]);

        Assert.Equal(new InvoiceNumber(2026, 6), numbering.Next(2026));
    }

    [Fact]
    public void Should_StartAtOneAgain_When_OnlyAnotherYearHasNumbers()
    {
        var numbering = new InvoiceNumbering([new InvoiceNumber(2025, 42), new InvoiceNumber(2027, 3)]);

        Assert.Equal(new InvoiceNumber(2026, 1), numbering.Next(2026));
    }
}
