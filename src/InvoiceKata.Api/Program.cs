using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Api;
using InvoiceKata.Api.Customers;
using InvoiceKata.Api.Issuers;
using InvoiceKata.Application.Customers;
using InvoiceKata.Application.Issuers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();

builder.Services.AddSingleton<IIssuerRepository, InMemoryIssuerRepository>();
builder.Services.AddTransient<ConfigureIssuer>();
builder.Services.AddTransient<GetIssuer>();
builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddTransient<RegisterCustomer>();
builder.Services.AddTransient<GetCustomer>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapIssuerEndpoints();
app.MapCustomerEndpoints();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
