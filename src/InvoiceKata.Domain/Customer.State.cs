namespace InvoiceKata.Domain;

public sealed record CustomerState(
    int Version, Guid Id, Guid IssuerId, CustomerKind Kind, string Name, string? Siren, string? VatNumber, Address Address)
{
    public const int CurrentVersion = 1;
}

public sealed partial class Customer : IPersistable<Customer, CustomerState>
{
    public static Customer FromState(CustomerState state)
    {
        StateGuard.RequireVersion(state.Version, CustomerState.CurrentVersion);
        StateGuard.Require(state.Id != Guid.Empty && state.IssuerId != Guid.Empty, "the customer or its issuer has no identifier");
        var hasCompanyIdentifiers = state.Kind switch
        {
            CustomerKind.Professional => state.Siren is not null && state.VatNumber is not null,
            CustomerKind.Individual => state.Siren is null && state.VatNumber is null,
            _ => throw new ArgumentException($"Invalid stored state: customer kind {state.Kind} is unknown.", nameof(state)),
        };
        StateGuard.Require(hasCompanyIdentifiers, $"the SIREN and VAT number do not fit the customer kind {state.Kind}");
        return new Customer(new CustomerId(state.Id), new IssuerId(state.IssuerId), state.Kind, state.Name,
            state.Siren is { } siren ? new Siren(siren) : null,
            state.VatNumber is { } vatNumber ? new VatNumber(vatNumber) : null,
            state.Address);
    }

    public CustomerState ToState() =>
        new(CustomerState.CurrentVersion, Id.Value, IssuerId.Value, Kind, Name, Siren?.Value, VatNumber?.Value, Address);
}
