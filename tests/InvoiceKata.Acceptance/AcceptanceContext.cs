using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Adapters.Rendering;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Invoices;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

namespace InvoiceKata.Acceptance;

// One instance per scenario (Reqnroll context injection): the use cases wired on in-memory adapters.
// The PDF is rendered by Typst for the scenarios tagged @pdf, which read it, and by a fake for the others.
public sealed class AcceptanceContext
{
    private readonly InMemoryIssuerRepository _issuers = new();
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryQuoteRepository _quotes = new();
    private readonly InMemoryInvoiceRepository _invoices = new();
    private readonly QuoteEventDispatcher _events;
    private readonly QuoteScopes _scopes;
    private readonly FrenchMentionTexts _texts = new();

    public AcceptanceContext(Reqnroll.ScenarioInfo scenario)
    {
        QuoteEventHandlers = [new IssuerLegalIdentityMention(_texts), new ReducedVatRateMention(_texts)];
        _events = new QuoteEventDispatcher(QuoteEventHandlers);
        _scopes = new QuoteScopes(_issuers, _customers);
        // The real Typst rendering for the @pdf scenarios: they read the PDF the reader would get.
        var validator = new FinalDocumentValidator(
            [new HeaderValidator(), new IssuerValidator(), new CustomerValidator(), new LinesValidator(), new TotalsValidator(), new MentionsValidator()]);
        IDocumentRenderer renderer = scenario.CombinedTags.Contains("pdf") ? new TypstRenderer("typst") : new FakeRenderer();
        var pdfs = new InMemoryPdfStore();
        RenderQuotePdf = new RenderQuotePdf(_quotes, _scopes, validator, renderer, pdfs);
        QuoteReactions = new QuoteReactions([RenderQuotePdf], Microsoft.Extensions.Logging.Abstractions.NullLogger<QuoteReactions>.Instance);
        RenderInvoicePdf = new RenderInvoicePdf(_invoices, _issuers, _customers, validator, renderer, pdfs);
        InvoiceReactions = new InvoiceReactions([RenderInvoicePdf], Microsoft.Extensions.Logging.Abstractions.NullLogger<InvoiceReactions>.Instance);
    }

    public ConfigureIssuer ConfigureIssuer => new(_issuers);

    public GetIssuer GetIssuer => new(_issuers);

    public RegisterCustomer RegisterCustomer => new(_issuers, _customers);

    public RegisterIndividualCustomer RegisterIndividualCustomer => new(_issuers, _customers);

    public GetCustomer GetCustomer => new(_customers);

    public CreateQuote CreateQuote => new(_issuers, _customers, _quotes, _events);

    public GetQuote GetQuote => new(_quotes);

    public QuoteScopes QuoteScopes => _scopes;

    public IReadOnlyList<IQuoteEventHandler> QuoteEventHandlers { get; }

    public AddQuoteLine AddQuoteLine => new(_quotes, _scopes, _events);

    public RemoveQuoteLine RemoveQuoteLine => new(_quotes, _scopes, _events);

    public FinalizeQuote FinalizeQuote => new(_quotes, _texts, Clock, QuoteReactions);

    public IssueInvoice IssueInvoice => new(_quotes, _invoices, _texts, Clock, InvoiceReactions);

    public GetInvoice GetInvoice => new(_invoices);

    public InvoiceReactions InvoiceReactions { get; }

    public RenderInvoicePdf RenderInvoicePdf { get; }

    public RenderQuotePdf RenderQuotePdf { get; }

    public QuoteReactions QuoteReactions { get; }

    public FixedClock Clock { get; } = new();

    // Scenarios name issuers ("Batifolio"); the steps resolve those names to identifiers.
    public Dictionary<string, IssuerId> IssuersByName { get; } = [];

    public Dictionary<string, CustomerId> CustomersByName { get; } = [];

    // The failure of the last "When" step, for the "Then … introuvable" steps.
    public Exception? LastFailure { get; set; }

    private sealed class FakeRenderer : IDocumentRenderer
    {
        public Task<byte[]> RenderAsync(ResolvedDocument document, CancellationToken cancellationToken) =>
            Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"%PDF-fake {document.Document.Number}"));
    }
}
