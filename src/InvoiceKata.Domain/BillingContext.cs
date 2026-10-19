namespace InvoiceKata.Domain;

public enum IssuerKind
{
    Company,
    IndividualEntrepreneur,
}

// Who bills whom. The product only handles the contexts it lists, and refuses any other (ADR-0002).
public readonly record struct BillingContext(IssuerKind Issuer, CustomerKind Customer)
{
    // The whitelist: adding a supported context is adding a line here, with its scenarios.
    private static readonly HashSet<BillingContext> Supported =
    [
        new(IssuerKind.Company, CustomerKind.Professional),
    ];

    public bool IsSupported => Supported.Contains(this);
}

public sealed class UnsupportedBillingContextException(BillingContext context)
    : Exception($"Billing context {context.Issuer} → {context.Customer} is not supported.")
{
    public BillingContext Context { get; } = context;
}
