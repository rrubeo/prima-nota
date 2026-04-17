using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.Ocr;

/// <summary><see cref="IReceiptAnalyzer"/> implementation using the OpenAI Chat Completions API with vision.</summary>
public sealed class OpenAiReceiptAnalyzer : IReceiptAnalyzer
{
    private const string ReceiptPrompt = """
        Analizza questa foto di uno scontrino o ricevuta e restituisci un JSON con questi campi:
        {
          "data": "YYYY-MM-DD",
          "importo": 12.50,
          "descrizione": "nome esercizio o descrizione breve",
          "categoria": "una tra: pasto, trasporto, carburante, alloggio, materiale, telefonia, abbonamento, altro"
        }
        Se un campo non è leggibile scrivi null. Restituisci SOLO il JSON, nessun altro testo.
        """;

    private static readonly Regex AmountRegex = new(@"""importo""\s*:\s*(\d+[.,]?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DateRegex = new(@"""data""\s*:\s*""(\d{4}-\d{2}-\d{2})""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DescRegex = new(@"""descrizione""\s*:\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CatRegex = new(@"""categoria""\s*:\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient http;
    private readonly OpenAiOptions options;
    private readonly ILogger<OpenAiReceiptAnalyzer> logger;

    /// <summary>Initializes a new instance of the <see cref="OpenAiReceiptAnalyzer"/> class.</summary>
    /// <param name="httpFactory">HTTP client factory.</param>
    /// <param name="options">OpenAI options.</param>
    /// <param name="logger">Logger.</param>
    public OpenAiReceiptAnalyzer(
        IHttpClientFactory httpFactory,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiReceiptAnalyzer> logger)
    {
        http = httpFactory.CreateClient("OpenAi");
        this.options = options.Value;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReceiptAnalysisResult> AnalyzeAsync(byte[] imageBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key non configurata (sezione OpenAi:ApiKey in appsettings).");
        }

        var base64 = Convert.ToBase64String(imageBytes);

        var payload = new
        {
            model = options.Model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = ReceiptPrompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:{mimeType};base64,{base64}" },
                        },
                    },
                },
            },
            max_tokens = 300,
        };

        var json = JsonSerializer.Serialize(payload);
        var apiUrl = $"{options.BaseUrl.TrimEnd('/')}/v1/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenAI API error {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"OpenAI API error: {response.StatusCode}");
        }

        return ParseResponse(body);
    }

    private static ReceiptAnalysisResult ParseResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var dataMatch = DateRegex.Match(content);
        var importoMatch = AmountRegex.Match(content);
        var descMatch = DescRegex.Match(content);
        var catMatch = CatRegex.Match(content);

        DateOnly? data = null;
        if (dataMatch.Success && DateOnly.TryParse(dataMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var d))
        {
            data = d;
        }

        decimal? importo = null;
        if (importoMatch.Success)
        {
            var clean = importoMatch.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(clean, CultureInfo.InvariantCulture, out var amt))
            {
                importo = amt;
            }
        }

        return new ReceiptAnalysisResult(
            data,
            importo,
            descMatch.Success ? descMatch.Groups[1].Value : null,
            catMatch.Success ? catMatch.Groups[1].Value : null);
    }
}
