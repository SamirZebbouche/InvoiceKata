using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceKata.Api.Tests;

public sealed class StartupTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Should_RefuseToStart_When_TypstIsNotConfigured(string? typst)
    {
        using var api = ApiFactory.On(Directory.CreateTempSubdirectory("invoicekata-startup-").FullName);
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("Persistence:Directory", api.DataDirectory)
            .UseSetting("Rendering:Typst", typst));

        var refusal = Record.Exception(() => factory.CreateClient());

        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains("Rendering:Typst", refusal.Message, StringComparison.Ordinal);
    }

    // A part left out would weaken the final validator in silence (ADR-0005, decision 9): the API composes them all.
    [Fact]
    public void Should_ComposeTheFinalValidatorOfEveryPartValidator_When_Starting()
    {
        using var api = ApiFactory.On(Directory.CreateTempSubdirectory("invoicekata-startup-").FullName);
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:Directory", api.DataDirectory));
        var every = typeof(InvoiceKata.Application.Rendering.IDocumentPartValidator).Assembly.GetTypes()
            .Where(type => typeof(InvoiceKata.Application.Rendering.IDocumentPartValidator).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false });

        var composed = factory.Services.GetServices<InvoiceKata.Application.Rendering.IDocumentPartValidator>().Select(part => part.GetType());

        Assert.Equal(every.OrderBy(type => type.Name), composed.OrderBy(type => type.Name));
    }

    // The other API tests fix the clock; without them, quotes are dated by the system clock.
    [Fact]
    public void Should_DateQuotesWithTheSystemClock_When_NothingReplacesIt()
    {
        using var api = ApiFactory.On(Directory.CreateTempSubdirectory("invoicekata-startup-").FullName);
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:Directory", api.DataDirectory));

        Assert.Same(TimeProvider.System, factory.Services.GetRequiredService<TimeProvider>());
    }

    // The default lives in appsettings.json only: without it, the API must not pick a directory on its own.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Should_RefuseToStart_When_TheDataDirectoryIsNotConfigured(string? directory)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:Directory", directory));

        var refusal = Record.Exception(() => factory.CreateClient());

        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains("Persistence:Directory", refusal.Message, StringComparison.Ordinal);
    }
}
