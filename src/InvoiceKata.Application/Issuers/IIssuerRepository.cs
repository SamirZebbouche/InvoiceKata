using InvoiceKata.Domain;

namespace InvoiceKata.Application.Issuers;

// Secondary port (ADR-0006).
public interface IIssuerRepository
{
    Task AddAsync(Issuer issuer, CancellationToken cancellationToken);

    /// <exception cref="IssuerNotFoundException">No issuer has this identifier.</exception>
    Task<Issuer> GetAsync(IssuerId id, CancellationToken cancellationToken);
}
