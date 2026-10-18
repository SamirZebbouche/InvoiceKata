using InvoiceKata.Application.Rendering;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceKata.Api.Tests;

// The API on its own data directory: a temporary one by default, removed afterwards; or a given one,
// so that a second instance can read what a first one stored (a restart). Its clock says 18/10/2026.
// It renders PDFs with a fake, unless asked for Typst: a real rendering costs a quarter of a second, and
// the tests that are not about the PDF have no use for it.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _ownsDirectory;
    private readonly bool _typst;

    public ApiFactory()
        : this(Directory.CreateTempSubdirectory("invoicekata-api-").FullName, ownsDirectory: true)
    {
    }

    // xUnit allows a fixture a single public constructor; a factory method opens an existing directory.
    private ApiFactory(string dataDirectory, bool ownsDirectory, bool typst = false)
    {
        DataDirectory = dataDirectory;
        _ownsDirectory = ownsDirectory;
        _typst = typst;
    }

    public string DataDirectory { get; }

    public static ApiFactory On(string dataDirectory) => new(dataDirectory, ownsDirectory: false);

    public static ApiFactory WithTypst() => new(Directory.CreateTempSubdirectory("invoicekata-api-").FullName, ownsDirectory: true, typst: true);

    public static DateOnly Today { get; } = new(2026, 10, 18);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Persistence:Directory", DataDirectory);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(new FixedClock());
            if (!_typst)
            {
                services.AddSingleton<IDocumentRenderer>(new FakeRenderer());
            }
        });
    }

    private sealed class FakeRenderer : IDocumentRenderer
    {
        public Task<byte[]> RenderAsync(ResolvedDocument document, CancellationToken cancellationToken) =>
            Task.FromResult(System.Text.Encoding.UTF8.GetBytes($"%PDF-fake {document.Document.Number}"));
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Today, new TimeOnly(10, 0), TimeSpan.Zero);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _ownsDirectory && Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
    }
}
