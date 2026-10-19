namespace InvoiceKata.Domain.Tests;

public sealed class PersistentStateTests
{
    [Fact]
    public void Should_RebuildTheSameQuoteWithoutRaisingEvents_When_RestoredFromItsState()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 33.33m, quantity: 18.72m, vatRate: 10m) with { Discount = new DiscountRate(10m) });
        quote.PutMention(new LegalMention(MentionCode.ReducedVatRate, "Taux réduit"));

        var restored = Quote.FromState(quote.ToState());

        Assert.Equivalent(quote.ToState(), restored.ToState(), strict: true);
        Assert.Equal(quote.TotalIncludingVat, restored.TotalIncludingVat);
        Assert.Empty(restored.TakeEvents());
    }

    [Fact]
    public void Should_RebuildTheSameIssuer_When_RestoredFromItsState()
    {
        var issuer = Quotes.Issuer();

        var restored = Issuer.FromState(issuer.ToState());

        Assert.Equal(issuer.ToState(), restored.ToState());
    }

    [Fact]
    public void Should_RebuildTheSameCustomer_When_RestoredFromItsState()
    {
        var customer = Quotes.ProfessionalCustomerOf(Quotes.Issuer());

        var restored = Customer.FromState(customer.ToState());

        Assert.Equal(customer.ToState(), restored.ToState());
    }

    [Fact]
    public void Should_RefuseTheState_When_ItsVersionIsUnknown()
    {
        var state = Quotes.Issuer().ToState() with { Version = 99 };

        Assert.Throws<ArgumentException>(() => Issuer.FromState(state));
    }

    [Fact]
    public void Should_RefuseTheState_When_AProfessionalCustomerHasNoSiren()
    {
        var state = Quotes.ProfessionalCustomerOf(Quotes.Issuer()).ToState() with { Siren = null };

        var refusal = Assert.Throws<ArgumentException>(() => Customer.FromState(state));
        Assert.Contains("the SIREN and VAT number do not fit the customer kind Professional", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_RefuseTheState_When_AnIndividualCustomerHasASiren()
    {
        var issuer = Quotes.Issuer();
        var individual = Customer.RegisterIndividual(
            issuer.Id, "Jeanne Martin", new Address { Street = "3 rue des Lilas", PostalCode = "33000", City = "Bordeaux" });
        var state = individual.ToState() with { Siren = "834567890" };

        var refusal = Assert.Throws<ArgumentException>(() => Customer.FromState(state));
        Assert.Contains("the SIREN and VAT number do not fit the customer kind Individual", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_RefuseTheState_When_TwoLinesShareAnIdentifier()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 100m));
        var state = quote.ToState();
        var duplicated = state with { Lines = [state.Lines[0], state.Lines[0]] };

        var refusal = Assert.Throws<ArgumentException>(() => Quote.FromState(duplicated));
        Assert.Contains("two lines share an identifier", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_RefuseTheState_When_TheIdentifierIsEmpty()
    {
        var state = Quotes.Draft().ToState() with { Id = Guid.Empty };

        Assert.Throws<ArgumentException>(() => Quote.FromState(state));
    }

    // Every structural invariant re-checked on restore has its own case: a guarantee nobody tests guarantees nothing.
    public static TheoryData<string, string, Func<object>> CorruptStates => new()
    {
        { "issuer of an unknown version", "version 3 is not supported", () => Issuer.FromState(Quotes.Issuer().ToState() with { Version = 3 }) },
        { "issuer without identifier", "the issuer has no identifier", () => Issuer.FromState(Quotes.Issuer().ToState() with { Id = Guid.Empty }) },
        { "issuer of an unknown legal form", "legal form 99 is unknown", () => Issuer.FromState(Quotes.Issuer().ToState() with { LegalForm = (LegalForm)99 }) },
        { "customer of an unknown version", "version 2 is not supported", () => Customer.FromState(CustomerState() with { Version = 2 }) },
        { "customer without identifier", "the customer or its issuer has no identifier", () => Customer.FromState(CustomerState() with { Id = Guid.Empty }) },
        { "customer without issuer", "the customer or its issuer has no identifier", () => Customer.FromState(CustomerState() with { IssuerId = Guid.Empty }) },
        { "customer of an unknown kind", "customer kind 99 is unknown", () => Customer.FromState(CustomerState() with { Kind = (CustomerKind)99 }) },
        { "quote of an unknown version", "version 4 is not supported", () => Quote.FromState(Quotes.Draft().ToState() with { Version = 4 }) },
        { "quote without issuer", "the quote, its issuer or its customer has no identifier", () => Quote.FromState(Quotes.Draft().ToState() with { IssuerId = Guid.Empty }) },
        { "quote without customer", "the quote, its issuer or its customer has no identifier", () => Quote.FromState(Quotes.Draft().ToState() with { CustomerId = Guid.Empty }) },
        { "quote of an unknown status", "status 99 is unknown", () => Quote.FromState(Quotes.Draft().ToState() with { Status = (QuoteStatus)99 }) },
        { "quote with an unknown mention code", "a mention code is unknown", () => Quote.FromState(Quotes.Draft().ToState() with { Mentions = [new LegalMention((MentionCode)99, "?")] }) },
        {
            "quote with two mentions of the same code", "two mentions share a code",
            () => Quote.FromState(Quotes.Draft().ToState() with
            {
                Mentions = [new LegalMention(MentionCode.ReducedVatRate, "a"), new LegalMention(MentionCode.ReducedVatRate, "b")],
            })
        },
    };

    [Theory]
    [MemberData(nameof(CorruptStates))]
    public void Should_RefuseToRestoreAndSayWhy_When_TheStateIsCorrupt(string corruption, string reason, Func<object> restore)
    {
        var refusal = Record.Exception(restore);

        Assert.True(refusal is ArgumentException, $"A {corruption} was restored instead of being refused.");
        Assert.Contains(reason, refusal.Message, StringComparison.Ordinal);
    }

    private static CustomerState CustomerState() => Quotes.ProfessionalCustomerOf(Quotes.Issuer()).ToState();
}
