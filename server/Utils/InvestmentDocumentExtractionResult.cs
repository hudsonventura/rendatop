using System.Text.Json.Serialization;

namespace server.Utils;

public class InvestmentDocumentExtractionResult
{
    public string? title { get; set; }
    public int? bank_code { get; set; }
    public string? bank_name { get; set; }
    public DateTime? date_buy { get; set; }
    public DateTime? due_date { get; set; }
    public decimal? value { get; set; }
    public int? index { get; set; }
    public decimal? index_percent { get; set; }
    public bool? taxes { get; set; }
    public bool? liquidez_diaria { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? notes { get; set; }
}
