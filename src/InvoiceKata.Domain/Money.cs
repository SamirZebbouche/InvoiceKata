namespace InvoiceKata.Domain;

// An amount in euros. Carries the rounding rule, so it is not a bare decimal.
public readonly record struct Money(decimal Amount)
{
    public static Money Zero { get; } = new(0m);

    // Commercial rounding: half a cent goes up (0,125 gives 0,13), not to the even cent as Math.Round does by default.
    public Money RoundedToTheCent() => new(Math.Round(Amount, 2, MidpointRounding.AwayFromZero));

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
}
