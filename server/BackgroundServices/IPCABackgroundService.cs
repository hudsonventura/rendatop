
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;

namespace server.BackgroundServices;

public class IPCABackgroundService : IHostedService
{
    Context _context;
    HttpClient _rest;
    ILogger _logger;
    void Wait() => Thread.Sleep((int) TimeSpan.FromHours(24).TotalMilliseconds);

    public IPCABackgroundService(ILogger logger, Context context)
    {
        Uri url = new Uri("https://api.bcb.gov.br");
        _rest = new HttpClient();
        _rest.BaseAddress = url;
        _rest.DefaultRequestHeaders.Add("Host", url.Host);
        _logger = logger;
        _context = context;
    }

    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task task = Task.Run(() => Run()); 
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }


    private Task Run()
    {
        DateOnly dtStart = GetLastDate();
        if(dtStart == DateOnly.MinValue){
            _logger.LogInformation($"Buscando TODAS as taxas IPCA");
            List<IPCA> ipcas = FethFromBCB();
            ipcas.RemoveAll(s => s.date > DateOnly.FromDateTime(DateTime.UtcNow));

            List<Selic> selicsFiltered = ipcas.Select(s => new Selic{date = s.date, value = s.value}).ToList();

            _context.ipcas.AddRange(ipcas);
            _context.SaveChanges();
            _logger.LogInformation($"{ipcas.Count()} registros obtidos");
            dtStart = ipcas.Max(x => x.date);
        }

        while (true){
            if(dtStart >= DateOnly.FromDateTime(DateTime.UtcNow)){
                Wait();
                continue;
            }
            

            try
            {
                _logger.LogInformation($"Buscando ultimas 10 taxas IPCA");
                List<IPCA> ipcas = FethFromBCB(10);
                ipcas.RemoveAll(s => s.date > DateOnly.FromDateTime(DateTime.UtcNow));
                var existingSelics = _context.selics.Where(s => ipcas.Select(x => x.date).Contains(s.date));
                foreach (var existing in existingSelics)
                {
                    ipcas.RemoveAll(s => s.date == existing.date);
                }
                _context.ipcas.AddRange(ipcas);
                _context.SaveChanges();
                _logger.LogInformation($"{ipcas.Count()} registros obtidos");
            }
            catch (System.Exception error)
            {
                _logger.LogError(error, "Erro ao obter taxa SELIC");
            }
            Wait();
        }
        return Task.CompletedTask;
    }


    private DateOnly GetLastDate(){
        return _context.ipcas.AsNoTracking().Any() ? _context.ipcas.Max(x => x.date) : DateOnly.MinValue;
    }
    

    internal List<IPCA> FethFromBCB(int last = 0)
    {
        string info = string.Empty;
        if(last >= 1){
            info = $"/ultimos/{last}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/dados/serie/bcdata.sgs.433/dados{info}?formato=json");
        request.Headers.Add("Accept", "application/json, text/plain, */*");

        //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = _rest.SendAsync(request).Result;
        string json = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            throw new ExpectedException(json, response.StatusCode);
        }

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement registros = document.RootElement;


            return registros.EnumerateArray().Select(registro => new IPCA
            {
                date = DateOnly.ParseExact(registro.GetProperty("data").GetString()!, "dd/MM/yyyy", null),
                value = decimal.Parse(registro.GetProperty("valor").GetString()!)
            }).ToList();
        }



        return null;
    }


}
