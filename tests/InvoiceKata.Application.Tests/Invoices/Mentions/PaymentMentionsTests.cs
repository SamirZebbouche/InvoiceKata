using InvoiceKata.Application.Invoices.Mentions;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Invoices.Mentions;

// The payment mentions every invoice carries (ADR-0010, ADR-0012).
public sealed class PaymentMentionsTests
{
    private static readonly FrenchMentionTexts Texts = new();
    private static readonly DateOnly DueOn = new(2026, 12, 30);

    [Fact]
    public void Should_StateTheDueDate_When_TheInvoiceIsIssued()
    {
        var mention = new PaymentDueDateMention(Texts).MentionFor(Scope());

        Assert.Equal(new LegalMention(MentionCode.PaymentDueDate, Texts.InvoiceDueDate(DueOn)), mention);
    }

    [Fact]
    public void Should_StateThatThereIsNoEarlyPaymentDiscount_When_TheInvoiceIsIssued()
    {
        var mention = new EarlyPaymentDiscountMention(Texts).MentionFor(Scope());

        Assert.Equal(new LegalMention(MentionCode.EarlyPaymentDiscount, Texts.NoEarlyPaymentDiscount()), mention);
    }

    [Fact]
    public void Should_StateTheLatePaymentPenalties_When_TheInvoiceIsIssued()
    {
        var mention = new LatePaymentPenaltiesMention(Texts).MentionFor(Scope());

        Assert.Equal(new LegalMention(MentionCode.LatePaymentPenalties, Texts.LatePaymentPenalties()), mention);
    }

    [Fact]
    public void Should_StateTheRecoveryIndemnity_When_TheCustomerIsAProfessional()
    {
        var mention = new RecoveryIndemnityMention(Texts).MentionFor(Scope());

        Assert.Equal(new LegalMention(MentionCode.RecoveryIndemnity, Texts.RecoveryIndemnity()), mention);
    }

    private static InvoiceScope Scope() => new(Scopes.Draft().Quote, DueOn);
}
