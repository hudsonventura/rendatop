using System.Text.Json;
using RendaTop.App.Models;

namespace RendaTop.App.Services;

public sealed class InvestmentCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string LastSyncKey = "rendatop.investments.last_sync_utc";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<InvestmentDto>? _cache;
    private bool _loadedFromDisk;
    private string CachePath => Path.Combine(FileSystem.AppDataDirectory, "investments-cache.json");

    public DateTimeOffset? LastSyncUtc
    {
        get
        {
            var raw = Preferences.Default.Get(LastSyncKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return DateTimeOffset.TryParse(raw, out var value) ? value : null;
        }
    }

    public async Task<IReadOnlyList<InvestmentDto>> GetOrFetchAsync(
        Func<CancellationToken, Task<IReadOnlyList<InvestmentDto>>> fetcher,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh)
            {
                if (!_loadedFromDisk)
                {
                    _cache = await ReadFromDiskAsync(cancellationToken);
                    _loadedFromDisk = true;
                }

                if (_cache is not null)
                    return _cache.ToList();
            }
        }
        finally
        {
            _gate.Release();
        }

        var fetched = (await fetcher(cancellationToken)).ToList();
        await SetAsync(fetched, cancellationToken);
        return fetched;
    }

    public async Task<IReadOnlyList<InvestmentDto>> GetCachedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_loadedFromDisk)
            {
                _cache = await ReadFromDiskAsync(cancellationToken);
                _loadedFromDisk = true;
            }

            return (_cache ?? []).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<InvestmentDto?> GetByIdAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        var items = await GetCachedAsync(cancellationToken);
        return items.FirstOrDefault(item => item.Id == investmentId);
    }

    public async Task SetAsync(IEnumerable<InvestmentDto> investments, CancellationToken cancellationToken = default)
    {
        var list = investments.ToList();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _cache = list;
            _loadedFromDisk = true;
            Preferences.Default.Set(LastSyncKey, DateTimeOffset.UtcNow.ToString("O"));
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
            await using var stream = File.Create(CachePath);
            await JsonSerializer.SerializeAsync(stream, _cache, JsonOptions, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_loadedFromDisk)
            {
                _cache = await ReadFromDiskAsync(cancellationToken);
                _loadedFromDisk = true;
            }

            var cache = _cache ??= [];
            cache.RemoveAll(item => item.Id == investmentId);
            await PersistUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ArchiveAsync(Guid investmentId, bool archived, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_loadedFromDisk)
            {
                _cache = await ReadFromDiskAsync(cancellationToken);
                _loadedFromDisk = true;
            }

            var cache = _cache ??= [];
            var index = cache.FindIndex(item => item.Id == investmentId);
            if (index < 0)
                return;

            cache[index] = cache[index] with { Archived = archived };
            await PersistUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertAsync(InvestmentDto investment, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_loadedFromDisk)
            {
                _cache = await ReadFromDiskAsync(cancellationToken);
                _loadedFromDisk = true;
            }

            var cache = _cache ??= [];
            var index = cache.FindIndex(item => item.Id == investment.Id);
            if (index >= 0)
                cache[index] = investment;
            else
                cache.Add(investment);

            await PersistUnsafeAsync(cancellationToken);
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
            _cache = [];
            _loadedFromDisk = true;
            Preferences.Default.Remove(LastSyncKey);

            if (File.Exists(CachePath))
                File.Delete(CachePath);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<InvestmentDto>> ReadFromDiskAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(CachePath))
            return [];

        await using var stream = File.OpenRead(CachePath);
        var items = await JsonSerializer.DeserializeAsync<List<InvestmentDto>>(stream, JsonOptions, cancellationToken);
        return items ?? [];
    }

    private async Task PersistUnsafeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
        await using var stream = File.Create(CachePath);
        await JsonSerializer.SerializeAsync(stream, _cache ?? [], JsonOptions, cancellationToken);
    }
}
