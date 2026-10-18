using System.ComponentModel;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using InvoiceKata.Application.Rendering;

namespace InvoiceKata.Adapters.Rendering;

// Renders through the Typst CLI, in a working folder of its own that is Typst's --root (ADR-0005): the
// template never sees a host path. The resolved document goes in as JSON (ADR-0005 amendment).
// The working folders go under workRoot, the system's temporary folder by default.
public sealed class TypstRenderer(string executable, string? workRoot = null) : IDocumentRenderer
{
    private const string Template = "societe.typ";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <exception cref="InvalidOperationException">Typst cannot be run, or fails to compile the document.</exception>
    public async Task<byte[]> RenderAsync(ResolvedDocument document, CancellationToken cancellationToken)
    {
        // A concatenation, not an interpolated string: emptying the literal still leaves a folder of its own, so no
        // mutant can turn the working folder into its root, which the finally block would then delete.
        // Stryker disable once String : equivalent, an emptied prefix or GUID format still names a folder of its own
        var work = Directory.CreateDirectory(Path.Combine(workRoot ?? Path.GetTempPath(), "invoicekata-render-" + Guid.NewGuid().ToString("N")));
        try
        {
            await using (var template = File.Create(Path.Combine(work.FullName, Template)))
            await using (var embedded = typeof(TypstRenderer).Assembly.GetManifestResourceStream($"Templates.{Template}")!)
            {
                await embedded.CopyToAsync(template, cancellationToken);
            }

            await File.WriteAllTextAsync(Path.Combine(work.FullName, "document.json"), JsonSerializer.Serialize(document, Json), cancellationToken);
            await CompileAsync(work.FullName, cancellationToken);
            return await File.ReadAllBytesAsync(Path.Combine(work.FullName, "document.pdf"), cancellationToken);
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    private async Task CompileAsync(string root, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = root,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { "compile", Template, "document.pdf", "--root", root, "--input", "data=/document.json" })
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process typst;
        try
        {
            typst = Process.Start(startInfo)!;
        }
        catch (Win32Exception notFound)
        {
            throw new InvalidOperationException($"Typst could not be run as '{executable}': install it, or set Rendering:Typst.", notFound);
        }

        using (typst)
        {
            var errors = await typst.StandardError.ReadToEndAsync(cancellationToken);
            await typst.WaitForExitAsync(cancellationToken);
            if (typst.ExitCode != 0)
            {
                throw new InvalidOperationException($"Typst failed to render the document ({typst.ExitCode}): {errors}");
            }
        }
    }
}
