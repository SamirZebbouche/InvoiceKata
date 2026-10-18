// Spike: render a resolved document (YAML) to PDF through the Typst CLI, from .NET.
// Usage: dotnet run --project spike/render -- <template.typ> <data.yaml> <output.pdf>
// All paths are relative to the spike folder, which is Typst's --root.
using System.Diagnostics;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: <template.typ> <data.yaml> <output.pdf> (relative to the spike folder)");
    return 2;
}

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var (template, data, output) = (args[0], args[1], args[2]);

var startInfo = new ProcessStartInfo("typst")
{
    WorkingDirectory = root,
    RedirectStandardError = true,
    UseShellExecute = false,
};
foreach (var argument in new[]
         {
             "compile", template, output, "--root", root,
             // Typst resolves "/…" from --root; the template never sees a host path.
             "--input", "data=/" + data.Replace('\\', '/'),
         })
{
    startInfo.ArgumentList.Add(argument);
}

var stopwatch = Stopwatch.StartNew();
using var typst = Process.Start(startInfo)!;
var errors = await typst.StandardError.ReadToEndAsync();
await typst.WaitForExitAsync();
stopwatch.Stop();

if (typst.ExitCode != 0)
{
    Console.Error.WriteLine($"typst failed ({typst.ExitCode}):{Environment.NewLine}{errors}");
    return 1;
}

var size = new FileInfo(Path.Combine(root, output)).Length;
Console.WriteLine($"{output}: {size / 1024} KiB in {stopwatch.ElapsedMilliseconds} ms");
return 0;
