namespace InvoiceKata.Domain;

// Something asked for does not exist, or belongs to another issuer; the API answers 404 either way (ADR-0007).
public abstract class ResourceNotFoundException(string message) : Exception(message);
