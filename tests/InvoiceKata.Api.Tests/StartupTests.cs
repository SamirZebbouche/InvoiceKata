using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceKata.Api.Tests;

public sealed class StartupTests
{
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
