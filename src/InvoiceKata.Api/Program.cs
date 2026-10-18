using InvoiceKata.Adapters.Persistence.InMemory;
using InvoiceKata.Api;
using InvoiceKata.Api.Issuers;
using InvoiceKata.Application.Issuers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();

builder.Services.AddSingleton<IIssuerRepository, InMemoryIssuerRepository>();
builder.Services.AddTransient<ConfigureIssuer>();
builder.Services.AddTransient<GetIssuer>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapIssuerEndpoints();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
