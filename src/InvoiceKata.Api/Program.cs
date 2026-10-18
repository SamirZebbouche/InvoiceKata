using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Api;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddSingleton<IIssuerRepository, InMemoryIssuerRepository>();
builder.Services.AddTransient<ConfigureIssuer>();
builder.Services.AddTransient<GetIssuer>();
builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddTransient<RegisterCustomer>();
builder.Services.AddTransient<RegisterIndividualCustomer>();
builder.Services.AddTransient<GetCustomer>();
builder.Services.AddSingleton<IQuoteRepository, InMemoryQuoteRepository>();
builder.Services.AddTransient<CreateQuote>();
builder.Services.AddTransient<GetQuote>();
builder.Services.AddTransient<AddQuoteLine>();
builder.Services.AddTransient<RemoveQuoteLine>();
builder.Services.AddTransient<QuoteScopes>();
builder.Services.AddTransient<QuoteEventDispatcher>();
builder.Services.AddSingleton<IMentionTexts, FrenchMentionTexts>();

// Each legal mention is a handler of its own: adding a mention is adding a line here (CDR-0001).
builder.Services.AddTransient<IQuoteEventHandler, IssuerLegalIdentityMention>();
builder.Services.AddTransient<IQuoteEventHandler, ReducedVatRateMention>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
