using InvoiceKata.Api.Issuers;
using InvoiceKata.Domain;

namespace InvoiceKata.Api.Tests;

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
}
