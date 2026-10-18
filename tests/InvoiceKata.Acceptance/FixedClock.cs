namespace InvoiceKata.Acceptance;

// "Nous sommes le 18/10/2026": the scenarios set the day. Mid-morning UTC is the same day in Paris, summer or winter.
public sealed class FixedClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);

    public void SetToday(DateOnly today) => _now = new DateTimeOffset(today, new TimeOnly(10, 0), TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;
}
