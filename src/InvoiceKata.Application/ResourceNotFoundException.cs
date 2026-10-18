namespace InvoiceKata.Application;

// A use case could not find what it was asked for; the API answers 404, also for another issuer's resource (ADR-0007).
public abstract class ResourceNotFoundException(string message) : Exception(message);
