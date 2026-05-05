
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

    private readonly List<string> _tags = new() { "IPCA", "BackgroundService" };
    private string _TraceId = string.Empty;


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
        _logger.LogInformation("Serviço de atualização da taxa IPCA iniciado. {TraceId} {_tags_}", _TraceId, _tags);


        
        DateOnly dtStart = GetLastDate();
        

        while (true){
            _TraceId = Guid.NewGuid().ToString();

            //Busca geral. Nada no banco de dados
            if(dtStart == DateOnly.MinValue){
                _logger.LogInformation("Buscando todas as taxas IPCA possiveis do BCB. TraceId={TraceId} {_tags_}", _TraceId, _tags);
                List<IPCA> ipcas = FethFromBCB();
                ipcas.RemoveAll(s => s.date > DateOnly.FromDateTime(DateTime.UtcNow));

                List<Selic> selicsFiltered = ipcas.Select(s => new Selic{date = s.date, value = s.value}).ToList();

                _context.ipcas.AddRange(ipcas);
                _context.SaveChanges();
                _logger.LogInformation("{Count} registros IPCA obtidos. TraceId={TraceId}", ipcas.Count(), _TraceId);
                dtStart = ipcas.Max(x => x.date);
            }
            else{


                _logger.LogInformation("Verificando atualização da taxa IPCA. Último registro em {lastDate} {TraceId} {_tags_}", dtStart, _TraceId, _tags);

                if(dtStart >= DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1))){
                    Wait();
                    continue;
                }
                

                try
                {
                    //Checa o mês anterior, pois o IPCA é divulgado com um mês de atraso
                    _logger.LogInformation("Buscando ultimas 3 taxas IPCA. TraceId={TraceId} {_tags_}", _TraceId, _tags);
                    List<IPCA> ipcas = FethFromBCB(3);
                    ipcas.RemoveAll(s => s.date > DateOnly.FromDateTime(DateTime.UtcNow));
                    var existingSelics = _context.ipcas.Where(s => ipcas.Select(x => x.date).Contains(s.date)).ToList();
                    foreach (var existing in existingSelics)
                    {
                        ipcas.RemoveAll(s => s.date == existing.date);
                    }

                    if(ipcas.Count() > 0){
                        _logger.LogInformation("Novas taxas IPCA encontradas: {Count}. Salvando no banco. TraceId={TraceId} {_tags_}", ipcas.Count(), _TraceId, _tags);
                        _context.ipcas.AddRange(ipcas);
                        _context.SaveChanges();
                        _logger.LogInformation("{Count} registros IPCA obtidos. TraceId={TraceId} {_tags_}", ipcas.Count(), _TraceId, _tags);
                    } else {
                        _logger.LogInformation("Nenhuma nova taxa IPCA encontrada. TraceId={TraceId} {_tags_}", _TraceId, _tags);
                    }
                    
                }
                catch (System.Exception error)
                {
                    _logger.LogError(error, "Erro ao obter taxa IPCA. TraceId={TraceId}", _TraceId);
                }
            }
            Wait();
        }

        _logger.LogError("Codigo do IPCA parou. Isso não deveria ter acontecido {TraceId} {_tags_}", _TraceId, _tags);

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
