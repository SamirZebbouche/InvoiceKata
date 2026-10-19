namespace InvoiceKata.Domain.Tests;

// The second kind of issuer (ADR-0011): an individual entrepreneur, in VAT franchise.
public sealed class IndividualEntrepreneurTests
{
    private static readonly Address Address = new() { Street = "8 chemin des Vignes", PostalCode = "47000", City = "Agen" };

    [Fact]
    public void Should_BeInVatFranchiseWithoutLegalFormCapitalNorVatNumber_When_ConfiguredAsAnIndividualEntrepreneur()
    {
        var issuer = Issuer.ConfigureIndividualEntrepreneur("Jean Dupont", new Siren("853123456"), Address);

        Assert.Equal(
            (IssuerKind.IndividualEntrepreneur, VatRegime.Franchise, "Jean Dupont", new Siren("853123456"), (LegalForm?)null, (decimal?)null, (VatNumber?)null),
            (issuer.Kind, issuer.VatRegime, issuer.Name, issuer.Siren, issuer.LegalForm, issuer.ShareCapital, issuer.VatNumber));
    }

    [Fact]
    public void Should_BeAtTheNormalVatRegime_When_ConfiguredAsACompany()
    {
        var issuer = Quotes.Issuer();

        Assert.Equal((IssuerKind.Company, VatRegime.Normal, (LegalForm?)LegalForm.Sas), (issuer.Kind, issuer.VatRegime, issuer.LegalForm));
    }

    [Fact]
    public void Should_RebuildTheSameIndividualEntrepreneur_When_RestoredFromItsState()
    {
        var issuer = Issuer.ConfigureIndividualEntrepreneur("Jean Dupont", new Siren("853123456"), Address);

        var restored = Issuer.FromState(issuer.ToState());

        Assert.Equal(issuer.ToState(), restored.ToState());
    }

    // Upcasting on read (ADR-0009): a version 1 issuer is a company at the normal VAT regime.
    [Fact]
    public void Should_ReadAVersion1IssuerAsACompanyAtTheNormalRegime_When_RestoringAnOlderState()
    {
        var version1 = Quotes.Issuer().ToState() with { Version = 1 };

        var restored = Issuer.FromState(version1);

        Assert.Equal((IssuerState.CurrentVersion, IssuerKind.Company, VatRegime.Normal), (restored.ToState().Version, restored.Kind, restored.VatRegime));
    }

    public static TheoryData<string, string, Func<object>> CorruptStates => new()
    {
        { "issuer of an unknown version", "version 3 is not supported", () => Issuer.FromState(Company() with { Version = 3 }) },
        { "issuer of an unknown kind", "issuer kind 99 is unknown", () => Issuer.FromState(Company() with { Kind = (IssuerKind)99 }) },
        { "issuer of an unknown VAT regime", "VAT regime 99 is unknown", () => Issuer.FromState(Company() with { VatRegime = (VatRegime)99 }) },
        { "company without legal form", "a company has a legal form, a share capital and a VAT number", () => Issuer.FromState(Company() with { LegalForm = null }) },
        { "company without capital", "a company has a legal form, a share capital and a VAT number", () => Issuer.FromState(Company() with { ShareCapital = null }) },
        { "company without VAT number", "a company has a legal form, a share capital and a VAT number", () => Issuer.FromState(Company() with { VatNumber = null }) },
        { "company in franchise", "a company is at the normal VAT regime", () => Issuer.FromState(Company() with { VatRegime = VatRegime.Franchise }) },
        { "individual entrepreneur with a legal form", "an individual entrepreneur has no legal form, share capital nor VAT number", () => Issuer.FromState(Entrepreneur() with { LegalForm = LegalForm.Sas }) },
        { "individual entrepreneur with a VAT number", "an individual entrepreneur has no legal form, share capital nor VAT number", () => Issuer.FromState(Entrepreneur() with { VatNumber = "FR12853123456" }) },
        { "individual entrepreneur at the normal regime", "an individual entrepreneur is in VAT franchise", () => Issuer.FromState(Entrepreneur() with { VatRegime = VatRegime.Normal }) },
        { "version 1 issuer that is not a company", "a version 1 issuer is a company at the normal VAT regime", () => Issuer.FromState(Entrepreneur() with { Version = 1 }) },
        { "version 1 individual entrepreneur at the normal regime", "a version 1 issuer is a company at the normal VAT regime", () => Issuer.FromState(Entrepreneur() with { Version = 1, VatRegime = VatRegime.Normal }) },
    };

    [Theory]
    [MemberData(nameof(CorruptStates))]
    public void Should_RefuseToRestoreAndSayWhy_When_TheStateIsCorrupt(string corruption, string reason, Func<object> restore)
    {
        var refusal = Record.Exception(restore);

        Assert.True(refusal is ArgumentException, $"An {corruption} was restored instead of being refused.");
        Assert.Contains(reason, refusal.Message, StringComparison.Ordinal);
    }

    private static IssuerState Company() => Quotes.Issuer().ToState();

    private static IssuerState Entrepreneur() => Issuer.ConfigureIndividualEntrepreneur("Jean Dupont", new Siren("853123456"), Address).ToState();
}
