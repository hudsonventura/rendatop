using System.Text.Json;

namespace RendaTop.App.Services;

public sealed class LocalSnapshotStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string RootPath => Path.Combine(FileSystem.AppDataDirectory, "offline-snapshots");

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
            return default;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(RootPath);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetPath(string key)
        => Path.Combine(RootPath, $"{SanitizeKey(key)}.json");

    private static string SanitizeKey(string key)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = key.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }
}
