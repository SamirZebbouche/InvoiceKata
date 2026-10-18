namespace InvoiceKata.Domain.Tests;

public sealed class LegalFormLabelsTests
{
    public static TheoryData<LegalForm> AllLegalForms => [.. Enum.GetValues<LegalForm>()];

    [Theory]
    [MemberData(nameof(AllLegalForms))]
    public void Should_ParseBackItsOwnLabel_When_GivenAnyLegalForm(LegalForm legalForm)
    {
        var label = LegalFormLabels.Of(legalForm);

        var parsed = LegalFormLabels.TryParse(label, out var roundTrip);

        Assert.True(parsed);
        Assert.Equal(legalForm, roundTrip);
    }

    // The labels are a contract: the API exchanges them and the mention texts print them.
    [Theory]
    [InlineData(LegalForm.Sa, "SA")]
    [InlineData(LegalForm.Sas, "SAS")]
    [InlineData(LegalForm.Sasu, "SASU")]
    [InlineData(LegalForm.Sarl, "SARL")]
    [InlineData(LegalForm.Eurl, "EURL")]
    [InlineData(LegalForm.Snc, "SNC")]
    public void Should_UseTheLegalAbbreviation_When_LabellingALegalForm(LegalForm legalForm, string label)
    {
        Assert.Equal(label, LegalFormLabels.Of(legalForm));
    }

    [Fact]
    public void Should_NotParse_When_TheLabelIsNoLegalForm()
    {
        var parsed = LegalFormLabels.TryParse("XYZ", out _);

        Assert.False(parsed);
    }
}
