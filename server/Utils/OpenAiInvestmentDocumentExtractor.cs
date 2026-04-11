using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using server.Domain;

namespace server.Utils;

public class OpenAiInvestmentDocumentExtractor : IInvestmentDocumentExtractor
{
    private const int MaxInlineTextCharacters = 18000;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiInvestmentDocumentExtractor> _logger;

    public OpenAiInvestmentDocumentExtractor(
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiInvestmentDocumentExtractor> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<InvestmentDocumentExtractionResult> ExtractAsync(
        IFormFile file,
        IReadOnlyCollection<Bank> banks,
        CancellationToken cancellationToken = default
    )
    {
        ValidateFile(file);

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ExpectedException("OPENAI_API_KEY não está configurada no servidor.", System.Net.HttpStatusCode.InternalServerError);

        var requestBody = await BuildRequestBodyAsync(file, banks, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("Accept", "application/json");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI extraction failed: {StatusCode} {Payload}", response.StatusCode, payload);
            throw new ExpectedException("Falha ao processar o arquivo com a IA.", System.Net.HttpStatusCode.BadGateway);
        }

        return ParseExtractionResult(payload);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file is null || file.Length <= 0)
            throw new ExpectedException("Envie um arquivo para extrair os dados.");

        if (file.Length > 10 * 1024 * 1024)
            throw new ExpectedException("O arquivo deve ter no máximo 10 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string> { ".txt", ".html", ".htm", ".pdf", ".png", ".jpg", ".jpeg", ".webp" };

        if (!allowedExtensions.Contains(extension))
            throw new ExpectedException("Formato não suportado. Envie txt, html, pdf ou imagem.");
    }

    private async Task<string> BuildRequestBodyAsync(
        IFormFile file,
        IReadOnlyCollection<Bank> banks,
        CancellationToken cancellationToken
    )
    {
        var model = Environment.GetEnvironmentVariable("OPENAI_INVESTMENT_EXTRACTION_MODEL");
        if (string.IsNullOrWhiteSpace(model))
            model = "gpt-4o-mini";

        var contentItems = new List<object>
        {
            new
            {
                type = "input_text",
                text = BuildUserPrompt(banks)
            }
        };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is ".txt" or ".html" or ".htm")
        {
            var textContent = await ReadTextFileAsync(file, cancellationToken);
            contentItems.Add(new
            {
                type = "input_text",
                text = $"Conteúdo do documento ({file.FileName}):\n{textContent}"
            });
        }
        else if (extension == ".pdf")
        {
            contentItems.Add(new
            {
                type = "input_file",
                filename = file.FileName,
                file_data = await BuildDataUrlAsync(file, cancellationToken)
            });
        }
        else
        {
            contentItems.Add(new
            {
                type = "input_image",
                image_url = await BuildDataUrlAsync(file, cancellationToken)
            });
        }

        var schema = new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                title = new { type = new[] { "string", "null" } },
                investment_type = new { type = new[] { "integer", "null" }, @enum = new int?[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, null } },
                bank_code = new { type = new[] { "integer", "null" } },
                bank_name = new { type = new[] { "string", "null" } },
                date_buy = new { type = new[] { "string", "null" }, description = "Data de compra no formato YYYY-MM-DD." },
                due_date = new { type = new[] { "string", "null" }, description = "Data de vencimento/resgate no formato YYYY-MM-DD." },
                value = new { type = new[] { "number", "null" } },
                index = new { type = new[] { "integer", "null" }, @enum = new int?[] { 0, 1, 2, 3, null } },
                index_percent = new { type = new[] { "number", "null" } },
                taxes = new { type = new[] { "boolean", "null" } },
                liquidez_diaria = new { type = new[] { "boolean", "null" } },
                notes = new { type = new[] { "string", "null" } }
            },
            required = new[]
            {
                "title", "investment_type", "bank_code", "bank_name", "date_buy", "due_date", "value",
                "index", "index_percent", "taxes", "liquidez_diaria", "notes"
            }
        };

        var body = new
        {
            model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = BuildSystemPrompt()
                        }
                    }
                },
                new
                {
                    role = "user",
                    content = contentItems
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "investment_document_extraction",
                    schema,
                    strict = true
                }
            }
        };

        return JsonSerializer.Serialize(body);
    }

    private static string BuildSystemPrompt()
    {
        return """
Você é um extrator de dados de comprovantes e contratos de investimento de renda fixa no Brasil.
Retorne somente os campos do schema solicitado.
Regras:
- Preencha apenas quando o documento trouxer a informação com boa confiança.
- Use null quando estiver ausente, ilegível ou ambígua.
- `investment_type`: 0=CDB, 1=LCI, 2=LCA, 3=RCI, 4=RCA, 5=Tesouro, 6=Debentures, 7=TitulosPublicos, 8=CRI, 9=CRA, 10=RDB.
- `index`: 0 para percentual do CDI (ex.: 110% do CDI), 1 para IPCA+ (ex.: IPCA + 7,25%), 2 para percentual ao ano fixo (% a.a., prefixado), 3 para CDI + spread (% a.a.) (ex.: CDI + 2,00% a.a.).
- `index_percent`: use somente o percentual principal do indexador. Exemplos: 110% do CDI => 110; IPCA+7,25% => 7.25; 13,40% a.a. => 13.40; CDI+2,00% a.a. => 2.00.
- Diferencie com cuidado `110% do CDI` de `CDI + 2,00% a.a.`: o primeiro é `index` 0 e o segundo é `index` 3.
- Interprete números no padrão brasileiro quando isso aparecer no documento: vírgula como separador decimal e ponto como separador de milhar. Exemplos: `1.234,56` => 1234.56; `13,40%` => 13.40; `100.000,00` => 100000.00.
- Podem aparecem alguns valores. Tenha certeza de extrair o valor investido/aplicado no recibo ou comprovante.
- Para `value`, priorize o valor efetivamente investido/aplicado no recibo ou comprovante, e normalize para número decimal no JSON.
- `taxes`: false para investimentos isentos como LCI, LCA, CRI, CRA, debênture incentivada; true para CDB, RDB e casos tributáveis; null se não der para inferir.
- `liquidez_diaria`: true apenas se o documento indicar liquidez diária/resgate diário; false se indicar vencimento fixo sem liquidez diária; null se não der para inferir.
- `title`: gere um título curto e útil quando houver dados suficientes, como produto + banco.
- Normalize datas para YYYY-MM-DD.
- `bank_code` deve ser um dos códigos da lista enviada quando houver correspondência clara; senão use null e tente preencher `bank_name`.
""";
    }

    private static string BuildUserPrompt(IReadOnlyCollection<Bank> banks)
    {
        var bankOptions = string.Join(
            ", ",
            banks
                .OrderBy(bank => bank.Name)
                .Select(bank => $"{bank.Code}={bank.Name}")
        );

        return $"""
Extraia os campos do investimento a partir do documento enviado.
Lista de bancos cadastrados para mapear `bank_code`: {bankOptions}
Se houver conflito entre múltiplos investimentos no mesmo arquivo, priorize o investimento principal/mais destacado.
Identifique também o `investment_type` quando o documento mencionar claramente CDB, LCI, LCA, RCI, RCA, Tesouro, Debêntures, Títulos Públicos, CRI, CRA ou RDB.
Ao identificar o indexador, diferencie percentual do CDI (ex.: 110% do CDI) de CDI + spread anual (ex.: CDI + 2,00% a.a.).
Considere que recibos brasileiros costumam usar vírgula como separador decimal em valores e percentuais.
""";
    }

    private static async Task<string> ReadTextFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var content = await reader.ReadToEndAsync(cancellationToken);

        if (Path.GetExtension(file.FileName).ToLowerInvariant() is ".html" or ".htm")
        {
            content = Regex.Replace(content, "<script[\\s\\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
            content = Regex.Replace(content, "<style[\\s\\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);
            content = Regex.Replace(content, "<[^>]+>", " ");
        }

        content = Regex.Replace(content, "\\s+", " ").Trim();
        if (content.Length > MaxInlineTextCharacters)
            content = content[..MaxInlineTextCharacters];

        return content;
    }

    private static async Task<string> BuildDataUrlAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();
        var mimeType = string.IsNullOrWhiteSpace(file.ContentType) ? GetMimeTypeFromExtension(file.FileName) : file.ContentType;
        return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string GetMimeTypeFromExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static InvestmentDocumentExtractionResult ParseExtractionResult(string payload)
    {
        using var document = JsonDocument.Parse(payload);

        string? outputText = null;
        if (document.RootElement.TryGetProperty("output_text", out var outputTextProperty) &&
            outputTextProperty.ValueKind == JsonValueKind.String)
        {
            outputText = outputTextProperty.GetString();
        }

        if (string.IsNullOrWhiteSpace(outputText) &&
            document.RootElement.TryGetProperty("output", out var outputArray) &&
            outputArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var outputItem in outputArray.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentArray) || contentArray.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textProperty) && textProperty.ValueKind == JsonValueKind.String)
                    {
                        outputText = textProperty.GetString();
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(outputText))
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(outputText))
            throw new ExpectedException("A IA não retornou dados estruturados do documento.", System.Net.HttpStatusCode.BadGateway);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var result = JsonSerializer.Deserialize<InvestmentDocumentExtractionResult>(outputText, options);
        if (result is null)
            throw new ExpectedException("Não foi possível interpretar a resposta da IA.", System.Net.HttpStatusCode.BadGateway);

        return result;
    }
}
