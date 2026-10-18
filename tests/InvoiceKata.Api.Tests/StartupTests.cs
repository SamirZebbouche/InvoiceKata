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
