using InvoiceKata.Application.Quotes;
using InvoiceKata.Domain;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceKata.Application.Tests.Quotes;

public sealed class QuoteReactionsTests
{
    private static readonly QuoteId Quote = QuoteId.New();
    private readonly List<(string Reaction, IQuoteEvent Event)> _received = [];

    [Fact]
    public async Task Should_GiveEveryEventToEveryReactionInOrder_When_Publishing()
    {
        IQuoteEvent[] events = [new QuoteDrafted(Quote), new QuoteLineAdded(Quote, QuoteLineId.New())];

        await new QuoteReactions([Recording("first"), Recording("second")], NullLogger<QuoteReactions>.Instance).PublishAsync(events, CancellationToken.None);

        Assert.Equal([("first", events[0]), ("second", events[0]), ("first", events[1]), ("second", events[1])], _received);
    }

    // A reaction follows a recorded fact: its failure is logged, and neither undoes the fact nor stops the others.
    [Fact]
    public async Task Should_LogTheFailureAndGoOn_When_AReactionFails()
    {
        var logger = new CountingLogger();
        IQuoteEvent drafted = new QuoteDrafted(Quote);

        var failure = await Record.ExceptionAsync(() =>
            new QuoteReactions([new FailingReaction(), Recording("after")], logger).PublishAsync([drafted], CancellationToken.None));

        Assert.Null(failure);
        Assert.Equal([("after", drafted)], _received);
        Assert.Contains("FailingReaction", Assert.Single(logger.Errors), StringComparison.Ordinal);
    }

    private RecordingReaction Recording(string name) => new(name, _received);

    private sealed class RecordingReaction(string name, List<(string, IQuoteEvent)> received) : IQuoteReaction
    {
        public Task ReactAsync(IQuoteEvent quoteEvent, CancellationToken cancellationToken)
        {
            received.Add((name, quoteEvent));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingReaction : IQuoteReaction
    {
        public Task ReactAsync(IQuoteEvent quoteEvent, CancellationToken cancellationToken) => throw new InvalidOperationException("Typst is down.");
    }

    private sealed class CountingLogger : ILogger<QuoteReactions>
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error && exception is not null)
            {
                Errors.Add(formatter(state, exception));
            }
        }
    }
}
