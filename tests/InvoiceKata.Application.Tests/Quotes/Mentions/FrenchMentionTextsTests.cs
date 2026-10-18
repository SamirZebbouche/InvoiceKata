using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Quotes.Mentions;

public sealed class FrenchMentionTextsTests
{
    private const char Space = ' ';
    private const char Group = ' ';

    private readonly FrenchMentionTexts _texts = new();

    [Fact]
    public void Should_StateTheNameLegalFormCapitalAndSiren_When_ComposingTheIssuerLegalIdentity()
    {
        var identity = _texts.IssuerLegalIdentity(Issuer(LegalForm.Sas, shareCapital: 20000m));

        Assert.Equal($"Batifolio, SAS au capital de 20{Group}000{Space}€, SIREN 912{Space}345{Space}678", identity);
    }

    [Theory]
    [InlineData(500, "500")]
    [InlineData(1000000, "1 000 000")]
    public void Should_GroupTheThousands_When_WritingTheShareCapital(decimal shareCapital, string written)
    {
        var identity = _texts.IssuerLegalIdentity(Issuer(LegalForm.Sas, shareCapital));

        Assert.Contains($"au capital de {written}{Space}€,", identity, StringComparison.Ordinal);
    }

    private static Issuer Issuer(LegalForm legalForm, decimal shareCapital) => InvoiceKata.Domain.Issuer.ConfigureCompany(
        "Batifolio", legalForm, shareCapital, new Siren("912345678"), new VatNumber("FR12912345678"),
        new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });
}
