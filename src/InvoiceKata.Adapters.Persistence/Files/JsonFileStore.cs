using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using InvoiceKata.Domain;

namespace InvoiceKata.Adapters.Persistence.Files;

// Stores aggregate states as JSON files under one directory (ADR-0006). Generic over IPersistable (CDR-0006):
// it never sees an aggregate's internals, only its state.
public sealed class JsonFileStore(string directory)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    // A single writer at a time: the API is one process (ADR-0006).
    private readonly SemaphoreSlim _writer = new(1, 1);

    public Task SaveAsync<TAggregate, TState>(string relativePath, TAggregate aggregate, CancellationToken cancellationToken)
        where TAggregate : IPersistable<TAggregate, TState> =>
        SaveAsync<TAggregate, TState>(relativePath, aggregate, _ => Task.CompletedTask, cancellationToken);

    // The guard runs under the writer lock, just before the write: what it checks cannot change in between.
    public async Task SaveAsync<TAggregate, TState>(
        string relativePath, TAggregate aggregate, Func<CancellationToken, Task> guard, CancellationToken cancellationToken)
        where TAggregate : IPersistable<TAggregate, TState>
    {
        var path = Path.Combine(directory, relativePath);
        await _writer.WaitAsync(cancellationToken);
        try
        {
            await guard(cancellationToken);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // Atomic write: a crash leaves either the old file or the new one, never half a file.
            var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
            await using (var stream = File.Create(temporary))
            {
                await JsonSerializer.SerializeAsync(stream, aggregate.ToState(), Json, cancellationToken);
            }

            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            _writer.Release();
        }
    }

    public async Task<TAggregate?> LoadAsync<TAggregate, TState>(string relativePath, CancellationToken cancellationToken)
        where TAggregate : class, IPersistable<TAggregate, TState>
    {
        var path = Path.Combine(directory, relativePath);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        var state = await JsonSerializer.DeserializeAsync<TState>(stream, Json, cancellationToken)
            ?? throw new InvalidDataException($"'{path}' holds no state.");
        return TAggregate.FromState(state);
    }

    // Every aggregate stored directly under a directory; an absent directory holds none.
    public async Task<IReadOnlyList<TAggregate>> LoadAllAsync<TAggregate, TState>(string relativeDirectory, CancellationToken cancellationToken)
        where TAggregate : class, IPersistable<TAggregate, TState>
    {
        var folder = Path.Combine(directory, relativeDirectory);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var loaded = new List<TAggregate>();
        foreach (var path in Directory.EnumerateFiles(folder).Where(path => path.EndsWith(".json", StringComparison.Ordinal)))
        {
            loaded.Add((await LoadAsync<TAggregate, TState>(Path.GetRelativePath(directory, path), cancellationToken))!);
        }

        return loaded;
    }
}
