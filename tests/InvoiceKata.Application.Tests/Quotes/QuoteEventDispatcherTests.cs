using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

namespace InvoiceKata.Application.Tests.Quotes;

public sealed class QuoteEventDispatcherTests
{
    private readonly List<(string Handler, Type Event)> _received = [];
    private readonly QuoteScope _scope = Scopes.Draft();

    [Fact]
    public void Should_GiveEveryPendingEventToEveryHandlerInOrder_When_Dispatching()
    {
        _scope.Quote.AddLine(Scopes.Line(vatRate: 10m));

        Dispatcher("first", "second").DispatchPendingEvents(_scope);

        Assert.Equal(
            [
                ("first", typeof(QuoteDrafted)), ("second", typeof(QuoteDrafted)),
                ("first", typeof(QuoteLineAdded)), ("second", typeof(QuoteLineAdded)),
            ],
            _received);
    }

    [Fact]
    public void Should_HandleEachEventOnce_When_DispatchingTwice()
    {
        var dispatcher = Dispatcher("only");
        dispatcher.DispatchPendingEvents(_scope);

        dispatcher.DispatchPendingEvents(_scope);

        Assert.Equal([("only", typeof(QuoteDrafted))], _received);
    }

    private QuoteEventDispatcher Dispatcher(params string[] handlers) =>
        new(handlers.Select(name => new RecordingHandler(name, _received)).ToList());

    private sealed class RecordingHandler(string name, List<(string, Type)> received) : IQuoteEventHandler
    {
        public void Handle(IQuoteEvent quoteEvent, QuoteScope scope) => received.Add((name, quoteEvent.GetType()));
    }
}
