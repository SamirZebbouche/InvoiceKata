namespace InvoiceKata.Application;

// Documents are dated in France, whatever the clock's time zone: at 23:30 UTC on 31 December, it is 1 January in Paris.
internal static class ParisCalendar
{
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Paris).DateTime);
}
