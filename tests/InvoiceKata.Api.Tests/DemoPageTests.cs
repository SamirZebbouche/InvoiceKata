using System.Net;

namespace InvoiceKata.Api.Tests;

// The demo front is a zone under differential rigour (CLAUDE.md): its machine oracle is that the API serves it.
public sealed class DemoPageTests
{
    [Fact]
    public async Task Should_ServeTheDemoPage_When_TheRootIsAsked()
    {
        using var api = new ApiFactory();

        var response = await api.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>InvoiceKata", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/app.js", "text/javascript")]
    [InlineData("/style.css", "text/css")]
    public async Task Should_ServeTheScriptAndTheStyle_When_ThePageAsksForThem(string path, string mediaType)
    {
        using var api = new ApiFactory();

        var response = await api.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
    }
}
