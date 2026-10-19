namespace InvoiceKata.Domain;

// Version 2 added the kind and the VAT regime, and made the company's fields optional (ADR-0011).
public sealed record IssuerState(
    int Version, Guid Id, IssuerKind Kind, VatRegime VatRegime, string Name, LegalForm? LegalForm, decimal? ShareCapital, string Siren,
    string? VatNumber, Address Address)
{
    public const int CurrentVersion = 2;
}

public sealed partial class Issuer : IPersistable<Issuer, IssuerState>
{
    public static Issuer FromState(IssuerState stored)
    {
        var state = Upcast(stored);
        StateGuard.RequireVersion(state.Version, IssuerState.CurrentVersion);
        StateGuard.Require(state.Id != Guid.Empty, "the issuer has no identifier");
        StateGuard.Require(Enum.IsDefined(state.Kind), $"issuer kind {state.Kind} is unknown");
        StateGuard.Require(Enum.IsDefined(state.VatRegime), $"VAT regime {state.VatRegime} is unknown");
        switch (state.Kind)
        {
            case IssuerKind.Company:
                StateGuard.Require(state.LegalForm is not null && state.ShareCapital is not null && state.VatNumber is not null,
                    "a company has a legal form, a share capital and a VAT number");
                StateGuard.Require(Enum.IsDefined(state.LegalForm!.Value), $"legal form {state.LegalForm} is unknown");
                StateGuard.Require(state.VatRegime == VatRegime.Normal, "a company is at the normal VAT regime");
                break;
            case IssuerKind.IndividualEntrepreneur:
                StateGuard.Require(state.LegalForm is null && state.ShareCapital is null && state.VatNumber is null,
                    "an individual entrepreneur has no legal form, share capital nor VAT number");
                StateGuard.Require(state.VatRegime == VatRegime.Franchise, "an individual entrepreneur is in VAT franchise");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stored), state.Kind, "This kind of issuer has no invariants.");
        }

        return new Issuer(new IssuerId(state.Id), state.Kind, state.VatRegime, state.Name, state.LegalForm, state.ShareCapital,
            new Siren(state.Siren), state.VatNumber is { } vatNumber ? new VatNumber(vatNumber) : null, state.Address);
    }

    public IssuerState ToState() =>
        new(IssuerState.CurrentVersion, Id.Value, Kind, VatRegime, Name, LegalForm, ShareCapital, Siren.Value, VatNumber?.Value, Address);

    // One converter per version, applied on read and never written back (ADR-0009). Version 2 only added the kind
    // and the regime, whose default values are a company and the normal regime: a version 1 file reads as is.
    private static IssuerState Upcast(IssuerState stored)
    {
        if (stored.Version != 1)
        {
            return stored;
        }

        StateGuard.Require(stored.Kind == IssuerKind.Company && stored.VatRegime == VatRegime.Normal,
            "a version 1 issuer is a company at the normal VAT regime");
        return stored with { Version = 2 };
    }
}
