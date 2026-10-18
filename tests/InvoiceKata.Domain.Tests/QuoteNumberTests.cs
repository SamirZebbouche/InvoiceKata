namespace InvoiceKata.Domain.Tests;

public sealed class QuoteNumberTests
{
    [Theory]
    [InlineData(2026, 1, "D-2026-0001")]
    [InlineData(2026, 42, "D-2026-0042")]
    [InlineData(2027, 12345, "D-2027-12345")]
    public void Should_WriteThePrefixTheYearAndAtLeastFourDigits_When_Written(int year, int sequence, string written)
    {
        Assert.Equal(written, new QuoteNumber(year, sequence).ToString());
    }

    [Theory]
    [InlineData("D-2026-0001", 2026, 1)]
    [InlineData("D-2027-12345", 2027, 12345)]
    public void Should_ReadTheYearAndTheSequence_When_Parsed(string written, int year, int sequence)
    {
        Assert.Equal(new QuoteNumber(year, sequence), QuoteNumber.Parse(written));
    }

    [Theory]
    [InlineData("")]
    [InlineData("F-2026-0001")]
    [InlineData("D-26-0001")]
    [InlineData("D-2026-001")]
    [InlineData("D-2026-0000")]
    [InlineData("D-2026-00a1")]
    [InlineData("D-2026-0001 ")]
    public void Should_RefuseTheText_When_ItIsNotAQuoteNumber(string text)
    {
        Assert.Throws<FormatException>(() => QuoteNumber.Parse(text));
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, -1)]
    [InlineData(999, 1)]
    [InlineData(10000, 1)]
    public void Should_RefuseTheNumber_When_TheYearOrTheSequenceIsOutOfRange(int year, int sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QuoteNumber(year, sequence));
    }
}
