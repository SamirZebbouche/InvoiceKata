namespace InvoiceKata.Domain;

public sealed record IssuerState(
    int Version, Guid Id, string Name, LegalForm LegalForm, decimal ShareCapital, string Siren, string VatNumber, Address Address)
{
    public const int CurrentVersion = 1;
}

public sealed partial class Issuer : IPersistable<Issuer, IssuerState>
{
    public static Issuer FromState(IssuerState state)
    {
        StateGuard.RequireVersion(state.Version, IssuerState.CurrentVersion);
        StateGuard.Require(state.Id != Guid.Empty, "the issuer has no identifier");
        StateGuard.Require(Enum.IsDefined(state.LegalForm), $"legal form {state.LegalForm} is unknown");
        return new Issuer(new IssuerId(state.Id), state.Name, state.LegalForm, state.ShareCapital,
            new Siren(state.Siren), new VatNumber(state.VatNumber), state.Address);
    }

    public IssuerState ToState() =>
        new(IssuerState.CurrentVersion, Id.Value, Name, LegalForm, ShareCapital, Siren.Value, VatNumber.Value, Address);
}
