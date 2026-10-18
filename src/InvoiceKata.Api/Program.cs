using InvoiceKata.Adapters.Persistence.Files;
using InvoiceKata.Adapters.Rendering;
using InvoiceKata.Api;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Application.Rendering;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
// Stryker disable once Statement : killed by the API tests when applied by hand; a shared test host never re-runs startup under a mutant
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Data survive a restart: JSON files under a configurable directory (ADR-0006), whose default lives in appsettings.json.
var configuredDirectory = builder.Configuration["Persistence:Directory"];
if (string.IsNullOrWhiteSpace(configuredDirectory))
{
    throw new InvalidOperationException("Persistence:Directory is not configured: set it to the directory that holds the data.");
}

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, configuredDirectory);
builder.Services.AddSingleton(new JsonFileStore(dataDirectory));
builder.Services.AddSingleton<IPdfStore>(new FilePdfStore(dataDirectory));

// The PDF is rendered by the Typst CLI (ADR-0005), whose path lives in appsettings.json.
var typst = builder.Configuration["Rendering:Typst"];
if (string.IsNullOrWhiteSpace(typst))
{
    throw new InvalidOperationException("Rendering:Typst is not configured: set it to the Typst executable.");
}

builder.Services.AddSingleton<IDocumentRenderer>(new TypstRenderer(typst));

builder.Services.AddSingleton<IIssuerRepository, FileIssuerRepository>();
builder.Services.AddTransient<ConfigureIssuer>();
builder.Services.AddTransient<GetIssuer>();
builder.Services.AddSingleton<ICustomerRepository, FileCustomerRepository>();
builder.Services.AddTransient<RegisterCustomer>();
builder.Services.AddTransient<RegisterIndividualCustomer>();
builder.Services.AddTransient<GetCustomer>();
builder.Services.AddSingleton<IQuoteRepository, FileQuoteRepository>();
builder.Services.AddTransient<CreateQuote>();
builder.Services.AddTransient<GetQuote>();
builder.Services.AddTransient<AddQuoteLine>();
builder.Services.AddTransient<RemoveQuoteLine>();
builder.Services.AddTransient<FinalizeQuote>();
builder.Services.AddSingleton(TimeProvider.System);

// After the save, the reactions to a recorded fact (ADR-0003): the PDF of a finalized quote, validated first (ADR-0005).
builder.Services.AddSingleton<IDocumentPartValidator, HeaderValidator>();
builder.Services.AddSingleton<IDocumentPartValidator, IssuerValidator>();
builder.Services.AddSingleton<IDocumentPartValidator, CustomerValidator>();
builder.Services.AddSingleton<IDocumentPartValidator, LinesValidator>();
builder.Services.AddSingleton<IDocumentPartValidator, TotalsValidator>();
builder.Services.AddSingleton<IDocumentPartValidator, MentionsValidator>();
builder.Services.AddSingleton<FinalDocumentValidator>();
builder.Services.AddTransient<RenderQuotePdf>();
builder.Services.AddTransient<IQuoteReaction, RenderQuotePdf>();
builder.Services.AddTransient<QuoteReactions>();
builder.Services.AddTransient<QuoteScopes>();
builder.Services.AddTransient<QuoteEventDispatcher>();
builder.Services.AddSingleton<IMentionTexts, FrenchMentionTexts>();

// Each legal mention is a handler of its own: adding a mention is adding a line here (CDR-0001).
// Stryker disable once Statement : killed by the API tests when applied by hand; a shared test host never re-runs startup under a mutant
builder.Services.AddTransient<IQuoteEventHandler, IssuerLegalIdentityMention>();
// Stryker disable once Statement : killed by the API tests when applied by hand; a shared test host never re-runs startup under a mutant
builder.Services.AddTransient<IQuoteEventHandler, ReducedVatRateMention>();

var app = builder.Build();

// Stryker disable once Statement : killed by the API tests when applied by hand; a shared test host never re-runs startup under a mutant
app.UseExceptionHandler();
app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
