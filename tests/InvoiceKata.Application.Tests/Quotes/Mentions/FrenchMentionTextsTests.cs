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

    // A capital converted from francs keeps its cents (50 000 F = 7 622,45 €): a legal mention must not round it.
    [Theory]
    [InlineData(7622.45, "7 622,45")]
    [InlineData(1000.5, "1 000,50")]
    public void Should_KeepTheCents_When_TheShareCapitalHasSome(decimal shareCapital, string written)
    {
        var identity = _texts.IssuerLegalIdentity(Issuer(LegalForm.Sarl, shareCapital));

        Assert.Contains($"SARL au capital de {written}{Space}€,", identity, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(30, "2026-11-17", "Devis valable 30 jours, jusqu'au 17/11/2026.")]
    [InlineData(1, "2026-10-19", "Devis valable 1 jour, jusqu'au 19/10/2026.")]
    public void Should_StateTheDurationAndTheLastDay_When_ComposingTheQuoteValidity(int validityDays, string validUntil, string expected)
    {
        Assert.Equal(expected, _texts.QuoteValidity(validityDays, DateOnly.Parse(validUntil, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Should_StateThePaymentTerms_When_ComposingTheInvoicePaymentMentions()
    {
        Assert.Equal(
            [
                "Échéance : 30/12/2026.",
                "Escompte pour paiement anticipé : néant.",
                "Pénalités de retard : trois fois le taux d'intérêt légal.",
                $"Indemnité forfaitaire pour frais de recouvrement : 40{Space}€.",
            ],
            [_texts.InvoiceDueDate(new DateOnly(2026, 12, 30)), _texts.NoEarlyPaymentDiscount(), _texts.LatePaymentPenalties(), _texts.RecoveryIndemnity()]);
    }

    // The individual entrepreneur's name, followed by "EI" (article R526-27 of the code de commerce, ADR-0011).
    [Fact]
    public void Should_StateTheNameFollowedByEIAndTheSiren_When_TheIssuerIsAnIndividualEntrepreneur()
    {
        var entrepreneur = InvoiceKata.Domain.Issuer.ConfigureIndividualEntrepreneur(
            "Jean Dupont", new Siren("853123456"), new Address { Street = "8 chemin des Vignes", PostalCode = "47000", City = "Agen" });

        Assert.Equal($"Jean Dupont EI, SIREN 853{Space}123{Space}456", _texts.IssuerLegalIdentity(entrepreneur));
    }

    // The reference in force since 1 September 2026 (ADR-0011).
    [Fact]
    public void Should_CiteTheCibs_When_ComposingTheVatFranchiseMention()
    {
        Assert.Equal("TVA non applicable, art. L. 223 et s. du code des impositions sur les biens et services (CIBS)", _texts.VatFranchise());
    }

    private static Issuer Issuer(LegalForm legalForm, decimal shareCapital) => InvoiceKata.Domain.Issuer.ConfigureCompany(
        "Batifolio", legalForm, shareCapital, new Siren("912345678"), new VatNumber("FR12912345678"),
        new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });
}
