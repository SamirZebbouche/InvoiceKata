using InvoiceKata.Application.Invoices;
using InvoiceKata.Domain;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceKata.Application.Tests.Invoices;

public sealed class InvoiceReactionsTests
{
    private static readonly InvoiceId Invoice = InvoiceId.New();
    private readonly List<(string Reaction, IInvoiceEvent Event)> _received = [];

    [Fact]
    public async Task Should_GiveEveryEventToEveryReactionInOrder_When_Publishing()
    {
        IInvoiceEvent[] events = [new InvoiceIssued(Invoice, IssuerId.New(), new InvoiceNumber(2026, 1)), new InvoiceIssued(Invoice, IssuerId.New(), new InvoiceNumber(2026, 2))];

        await new InvoiceReactions([Recording("first"), Recording("second")], NullLogger<InvoiceReactions>.Instance).PublishAsync(events, CancellationToken.None);

        Assert.Equal([("first", events[0]), ("second", events[0]), ("first", events[1]), ("second", events[1])], _received);
    }

    // A reaction follows a recorded fact: its failure is logged, and neither undoes the fact nor stops the others.
    [Fact]
    public async Task Should_LogTheFailureAndGoOn_When_AReactionFails()
    {
        var logger = new CountingLogger();
        IInvoiceEvent drafted = new InvoiceIssued(Invoice, IssuerId.New(), new InvoiceNumber(2026, 1));

        var failure = await Record.ExceptionAsync(() =>
            new InvoiceReactions([new FailingReaction(), Recording("after")], logger).PublishAsync([drafted], CancellationToken.None));

        Assert.Null(failure);
        Assert.Equal([("after", drafted)], _received);
        Assert.Contains("FailingReaction", Assert.Single(logger.Errors), StringComparison.Ordinal);
    }

    private RecordingReaction Recording(string name) => new(name, _received);

    private sealed class RecordingReaction(string name, List<(string, IInvoiceEvent)> received) : IInvoiceReaction
    {
        public Task ReactAsync(IInvoiceEvent quoteEvent, CancellationToken cancellationToken)
        {
            received.Add((name, quoteEvent));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingReaction : IInvoiceReaction
    {
        public Task ReactAsync(IInvoiceEvent quoteEvent, CancellationToken cancellationToken) => throw new InvalidOperationException("Typst is down.");
    }

    private sealed class CountingLogger : ILogger<InvoiceReactions>
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
