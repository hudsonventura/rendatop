
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

namespace server.BackgroundServices;

public class SelicBackgroundService : IHostedService
{
    readonly Context _context;
    readonly HttpClient _rest;
    readonly ILogger _logger;
    readonly List<string> _tags = new List<string>();

    // The BCB Selic daily series started on this date (série 432)
    static readonly DateOnly SeriesStartDate = new DateOnly(1986, 6, 4);

    // BCB API limit: maximum 10-year window per request
    static readonly int MaxYearsPerRequest = 9;


    void Wait() => Thread.Sleep((int)TimeSpan.FromHours(1).TotalMilliseconds);

    public SelicBackgroundService(ILogger logger, Context context)
    {
        Uri url = new Uri("https://api.bcb.gov.br");
        _rest = new HttpClient();
        _rest.BaseAddress = url;
        _rest.DefaultRequestHeaders.Add("Host", url.Host);
        _logger = logger;
        _context = context;

        _tags.AddRange(["Selic", "SelicBackgroundService"]);
    }

    protected string GetTraceId() {
        string taskName = "background.selic";
        using var activity = TraceContext.StartActivity(taskName);
        return activity.Id;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task task = Task.Run(() => Run());
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }


    public Task Run()
    {
        string traceId = string.Empty;
        
        DateOnly lastDate = GetLastDate();
        _logger.LogInformation("Serviço de atualização da taxa SELIC iniciado. Último registro em {lastDate} {TraceId} {_tags_}", lastDate, traceId, _tags);

        if (lastDate == DateOnly.MinValue)
        {
            // DB is empty — fetch ALL historical data in 10-year chunks
            FetchAllHistoricalData();
            lastDate = GetLastDate();
        }

        // Incremental hourly update loop
        while (true)
        {
            traceId = Guid.NewGuid().ToString();

            _logger.LogInformation("Verificando atualização da taxa SELIC. {TraceId} {_tags_}", traceId, _tags);
            if (lastDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            {
                _logger.LogInformation("Taxa SELIC já está atualizada até hoje. Próxima verificação em 1 hora {TraceId} {_tags_}", traceId, _tags);
                Wait();
                continue;
            }

            List<Selic> selics = new List<Selic>();
            try
            {
                DateOnly to = lastDate.AddDays(MaxYearsPerRequest * 365);
                if(to > DateOnly.FromDateTime(DateTime.UtcNow))
                    to = DateOnly.FromDateTime(DateTime.UtcNow);

                _logger.LogInformation("Buscando ultimas taxas SELIC de {lastDate} até {to} {TraceId} {_tags_}", lastDate, to, traceId, _tags);
                selics = FetchFromBCB(lastDate, to); // fetch next chunk starting from lastDate + 10 years  
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Erro ao obter taxa SELIC {TraceId} {_tags_}", traceId, _tags);
            }

            try
            {
                selics = FilterOutExisting(selics);

                if (selics.Count > 0)
                {
                    _context.selics.AddRange(selics);
                    _context.SaveChanges();
                    _logger.LogInformation("{Count} novos registros SELIC salvos de {lastDate} até {now} {TraceId} {_tags_}", selics.Count, lastDate, selics.Max(x => x.date), traceId, _tags);
                    lastDate = selics.Max(x => x.date);
                }
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Erro ao salvar {Count} taxa SELIC no banco {TraceId} {_tags_}", selics.Count, traceId, _tags);
            }

            Wait();
        }

        _logger.LogError("Codigo da SELIC parou. Isso não deveria ter acontecido {TraceId} {_tags_}", traceId, _tags);

        return Task.CompletedTask;
    }


    /// <summary>
    /// Fetches all Selic data from the series start date to today,
    /// looping in 10-year chunks to respect the BCB API limit.
    /// </summary>
    private void FetchAllHistoricalData()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly chunkStart = SeriesStartDate;
        int totalSaved = 0;

        var traceId = TraceContext.GetTraceId();
        _logger.LogInformation("Iniciando busca completa da SELIC. TraceId={TraceId} DataInicial={DataInicial} {_tags_}", traceId, chunkStart.ToString("dd/MM/yyyy"), _tags);

        while (chunkStart < today)
        {
            DateOnly chunkEnd = chunkStart.AddYears(MaxYearsPerRequest);
            if (chunkEnd > today)
                chunkEnd = today;

            _logger.LogInformation("Buscando SELIC. TraceId={TraceId} Inicio={Inicio} Fim={Fim} {_tags_}", traceId, chunkStart.ToString("dd/MM/yyyy"), chunkEnd.ToString("dd/MM/yyyy"), _tags);

            try
            {
                List<Selic> selics = FetchFromBCB(chunkStart, chunkEnd);
                selics = FilterOutExisting(selics);

                if (selics.Count > 0)
                {
                    int saved = SaveInBatches(selics);
                    totalSaved += saved;
                    _logger.LogInformation("Registros SELIC salvos. TraceId={TraceId} Saved={Saved} Total={Total} {_tags_}", traceId, saved, totalSaved, _tags);
                }
                else
                {
                    _logger.LogInformation("Nenhum registro novo de SELIC neste periodo. TraceId={TraceId} {_tags_}", traceId, _tags);
                }
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Erro ao buscar SELIC no periodo. TraceId={TraceId} Inicio={Inicio} Fim={Fim} {_tags_}", traceId, chunkStart.ToString("dd/MM/yyyy"), chunkEnd.ToString("dd/MM/yyyy"), _tags);
            }

            chunkStart = chunkEnd.AddDays(1);
        }

        _logger.LogInformation("Busca completa da SELIC finalizada. TraceId={TraceId} Total={Total} {_tags_}", traceId, totalSaved, _tags);
    }


    /// <summary>
    /// Saves a list of Selic records in batches of 500 to avoid holding a long DB lock.
    /// Returns the total number of rows saved.
    /// </summary>
    private int SaveInBatches(List<Selic> selics, int batchSize = 500)
    {
        int total = 0;
        for (int i = 0; i < selics.Count; i += batchSize)
        {
            var batch = selics.Skip(i).Take(batchSize).ToList();
            _context.selics.AddRange(batch);
            _context.SaveChanges();
            total += batch.Count;
            _logger.LogDebug("Batch salvo: {Count} registros (offset {Offset}) {_tags_}", batch.Count, i, _tags);
        }
        return total;
    }


    private DateOnly GetLastDate()
    {
        return _context.selics.AsNoTracking().Any()
            ? _context.selics.AsNoTracking().Max(x => x.date)
            : DateOnly.MinValue;
    }


    /// <summary>
    /// Removes from the list any records that already exist in the database.
    /// </summary>
    private List<Selic> FilterOutExisting(List<Selic> selics)
    {
        var dates = selics.Select(s => s.date).ToList();
        var existingDates = _context.selics
            .Where(s => dates.Contains(s.date))
            .Select(s => s.date)
            .ToHashSet();

        return selics.Where(s => !existingDates.Contains(s.date)).ToList();
    }


    /// <summary>
    /// Fetches Selic records from the BCB API for the given date window.
    /// The window must not exceed 10 years (BCB API restriction).
    /// </summary>
    internal List<Selic> FetchFromBCB(DateOnly start, DateOnly end)
    {
        string datas = $"&dataInicial={start.ToString("dd/MM/yyyy")}&dataFinal={end.ToString("dd/MM/yyyy")}";

        var request = new HttpRequestMessage(HttpMethod.Get, $"/dados/serie/bcdata.sgs.432/dados?formato=json{datas}");
        request.Headers.Add("Accept", "application/json, text/plain, */*");

        HttpResponseMessage response = _rest.SendAsync(request).Result;
        string json = response.Content.ReadAsStringAsync().Result;

        // 404 means "no data found for this date range" — treat as empty chunk, not an error
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("BCB API returned 404 (no data) for window {Start} - {End}. Skipping. {_tags_}", start.ToString("dd/MM/yyyy"), end.ToString("dd/MM/yyyy"), _tags);
            return new List<Selic>();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExpectedException(json, response.StatusCode);
        }

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement registros = document.RootElement;

            return registros.EnumerateArray().Select(registro => new Selic
            {
                date = DateOnly.ParseExact(registro.GetProperty("data").GetString()!, "dd/MM/yyyy", null),
                value = decimal.Parse(registro.GetProperty("valor").GetString()!, System.Globalization.CultureInfo.InvariantCulture)
            }).ToList();
        }
    }
}
