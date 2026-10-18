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

        Assert.Equal(quote.ToState(), restored.ToState());
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

        Assert.Throws<ArgumentException>(() => Customer.FromState(state));
    }

    [Fact]
    public void Should_RefuseTheState_When_AnIndividualCustomerHasASiren()
    {
        var issuer = Quotes.Issuer();
        var individual = Customer.RegisterIndividual(
            issuer.Id, "Jeanne Martin", new Address { Street = "3 rue des Lilas", PostalCode = "33000", City = "Bordeaux" });
        var state = individual.ToState() with { Siren = "834567890" };

        Assert.Throws<ArgumentException>(() => Customer.FromState(state));
    }

    [Fact]
    public void Should_RefuseTheState_When_TwoLinesShareAnIdentifier()
    {
        var quote = Quotes.Draft();
        quote.AddLine(Quotes.Line(unitPrice: 100m));
        var state = quote.ToState();
        var duplicated = state with { Lines = [state.Lines[0], state.Lines[0]] };

        Assert.Throws<ArgumentException>(() => Quote.FromState(duplicated));
    }

    [Fact]
    public void Should_RefuseTheState_When_TheIdentifierIsEmpty()
    {
        var state = Quotes.Draft().ToState() with { Id = Guid.Empty };

        Assert.Throws<ArgumentException>(() => Quote.FromState(state));
    }
}
