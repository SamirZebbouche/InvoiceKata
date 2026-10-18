using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceKata.Api.Tests;

// The API on its own data directory: a temporary one by default, removed afterwards; or a given one,
// so that a second instance can read what a first one stored (a restart). Its clock says 18/10/2026.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _ownsDirectory;

    public ApiFactory()
        : this(Directory.CreateTempSubdirectory("invoicekata-api-").FullName, ownsDirectory: true)
    {
    }

    // xUnit allows a fixture a single public constructor; a factory method opens an existing directory.
    private ApiFactory(string dataDirectory, bool ownsDirectory)
    {
        DataDirectory = dataDirectory;
        _ownsDirectory = ownsDirectory;
    }

    public string DataDirectory { get; }

    public static ApiFactory On(string dataDirectory) => new(dataDirectory, ownsDirectory: false);

    public static DateOnly Today { get; } = new(2026, 10, 18);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Persistence:Directory", DataDirectory);
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FixedClock()));
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
