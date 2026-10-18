using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Api;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;
using InvoiceKata.Application.Quotes;

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

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
