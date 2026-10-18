namespace InvoiceKata.Domain;

// A class that can be stored must offer both ways, which the compiler checks (CDR-0006).
public interface IPersistable<TSelf, TState>
    where TSelf : IPersistable<TSelf, TState>
{
    static abstract TSelf FromState(TState state);

    TState ToState();
}

internal static class StateGuard
{
    public static void Require(bool condition, string reason)
    {
        if (!condition)
        {
            throw new ArgumentException($"Invalid stored state: {reason}.", "state");
        }
    }

    public static void RequireVersion(int version, int supported) =>
        Require(version == supported, $"version {version} is not supported, only {supported} is");
}
