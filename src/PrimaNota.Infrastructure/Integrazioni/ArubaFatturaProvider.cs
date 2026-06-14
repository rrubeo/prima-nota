using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.PrimaNota.Import;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Infrastructure.Integrazioni;

/// <summary>
/// <see cref="IFatturaProvider"/> for the Aruba "Fatturazione Elettronica" REST API (v2).
/// Handles OAuth2 password-grant token caching, the API's 2-day date-window limit (with
/// throttling under the 12 requests/minute cap), pagination and CAdES (<c>.p7m</c>) unwrapping.
/// Credentials are read (and decrypted) from the database on demand.
/// </summary>
public sealed class ArubaFatturaProvider : IFatturaProvider, IDisposable
{
    private const string HttpClientName = "Aruba";

    // Stay safely under the documented 12 find-requests/minute limit.
    private static readonly TimeSpan ThrottleDelay = TimeSpan.FromSeconds(5);

    private static readonly (string Auth, string Ws) Production =
        ("https://auth.fatturazioneelettronica.aruba.it", "https://ws.fatturazioneelettronica.aruba.it");

    private static readonly (string Auth, string Ws) Demo =
        ("https://demoauth.fatturazioneelettronica.aruba.it", "https://demows.fatturazioneelettronica.aruba.it");

    private readonly IHttpClientFactory httpFactory;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ISecretProtector protector;
    private readonly IDateTimeProvider clock;
    private readonly ILogger<ArubaFatturaProvider> logger;

    private readonly SemaphoreSlim authLock = new(1, 1);
    private string? cachedToken;
    private bool cachedDemo;
    private DateTimeOffset tokenExpiresAt;

    /// <summary>Initializes a new instance of the <see cref="ArubaFatturaProvider"/> class.</summary>
    /// <param name="httpFactory">HTTP client factory.</param>
    /// <param name="scopeFactory">Scope factory used to read credentials from the scoped DB context.</param>
    /// <param name="protector">Secret protector used to decrypt the stored password.</param>
    /// <param name="clock">Clock.</param>
    /// <param name="logger">Logger.</param>
    public ArubaFatturaProvider(
        IHttpClientFactory httpFactory,
        IServiceScopeFactory scopeFactory,
        ISecretProtector protector,
        IDateTimeProvider clock,
        ILogger<ArubaFatturaProvider> logger)
    {
        this.httpFactory = httpFactory;
        this.scopeFactory = scopeFactory;
        this.protector = protector;
        this.clock = clock;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
        await ReadCredentialsAsync(cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FatturaRemota>> ListAsync(
        DirezioneFattura direzione,
        DateOnly da,
        DateOnly a,
        CancellationToken cancellationToken = default)
    {
        var creds = await ReadCredentialsAsync(cancellationToken)
            ?? throw new InvalidOperationException("Integrazione Aruba non configurata o disabilitata.");

        var segment = direzione == DirezioneFattura.Attiva ? "invoices-out" : "invoices-in";
        var (_, ws) = creds.UsaDemo ? Demo : Production;
        var client = httpFactory.CreateClient(HttpClientName);

        var results = new List<FatturaRemota>();
        var firstCall = true;

        foreach (var (start, end) in BuildWindows(da, a))
        {
            var page = 1;
            while (true)
            {
                if (!firstCall)
                {
                    await Task.Delay(ThrottleDelay, cancellationToken);
                }

                firstCall = false;

                var url = $"{ws}/api/v2/{segment}?creationStartDate={Uri.EscapeDataString(start)}" +
                          $"&creationEndDate={Uri.EscapeDataString(end)}&page={page}&size=100";
                using var doc = await SendJsonAsync(client, HttpMethod.Get, url, creds, cancellationToken);
                var root = doc.RootElement;

                if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in content.EnumerateArray())
                    {
                        results.Add(MapItem(item, direzione));
                    }
                }

                var last = root.TryGetProperty("last", out var l) && l.ValueKind == JsonValueKind.True;
                var totalPages = root.TryGetProperty("totalPages", out var tp) && tp.TryGetInt32(out var n) ? n : page;
                if (last || page >= totalPages)
                {
                    break;
                }

                page++;
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<FatturaScaricata> DownloadAsync(
        DirezioneFattura direzione,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var creds = await ReadCredentialsAsync(cancellationToken)
            ?? throw new InvalidOperationException("Integrazione Aruba non configurata o disabilitata.");

        var segment = direzione == DirezioneFattura.Attiva ? "invoices-out" : "invoices-in";
        var (_, ws) = creds.UsaDemo ? Demo : Production;
        var client = httpFactory.CreateClient(HttpClientName);

        var url = $"{ws}/api/v2/{segment}/detail?id={Uri.EscapeDataString(id)}&includeFile=true";
        using var doc = await SendJsonAsync(client, HttpMethod.Get, url, creds, cancellationToken);
        var root = doc.RootElement;

        var base64 = root.TryGetProperty("file", out var f) ? f.GetString() : null;
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new InvalidOperationException($"Aruba non ha restituito il contenuto della fattura {id}.");
        }

        var fileName = root.TryGetProperty("filename", out var fn) ? fn.GetString() : null;
        var idSdi = ReadString(root, "idSdi") ?? fileName ?? id;
        var xml = FatturaXmlExtractor.ExtractXml(Convert.FromBase64String(base64), fileName);

        return new FatturaScaricata(idSdi, xml);
    }

    /// <inheritdoc />
    public void Dispose() => authLock.Dispose();

    /// <summary>
    /// Splits an inclusive day range into windows of at most two days (the Aruba API limit),
    /// each expressed as an ISO-8601 [start, end) pair on UTC midnight boundaries.
    /// </summary>
    /// <param name="da">Range start (inclusive).</param>
    /// <param name="a">Range end (inclusive).</param>
    /// <returns>The list of (creationStartDate, creationEndDate) ISO strings.</returns>
    public static IReadOnlyList<(string Start, string End)> BuildWindows(DateOnly da, DateOnly a)
    {
        var windows = new List<(string, string)>();
        if (a < da)
        {
            return windows;
        }

        var s = da;
        while (s <= a)
        {
            var e = s.AddDays(2);
            var lastMidnight = a.AddDays(1);
            if (e > lastMidnight)
            {
                e = lastMidnight;
            }

            windows.Add((Iso(s), Iso(e)));
            s = s.AddDays(2);
        }

        return windows;
    }

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00.000Z";

    private static FatturaRemota MapItem(JsonElement item, DirezioneFattura direzione)
    {
        var id = ReadString(item, "id") ?? string.Empty;
        var idSdi = ReadString(item, "idSdi") ?? ReadString(item, "filename") ?? id;

        string? numero = null;
        var data = default(DateOnly);
        if (item.TryGetProperty("invoices", out var invoices)
            && invoices.ValueKind == JsonValueKind.Array
            && invoices.GetArrayLength() > 0)
        {
            var first = invoices[0];
            numero = ReadString(first, "number");
            if (ReadString(first, "invoiceDate") is { } ds
                && DateTimeOffset.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            {
                data = DateOnly.FromDateTime(dto.UtcDateTime);
            }
        }

        // Passive: the counterparty is the sender; active: the receiver.
        var partyName = direzione == DirezioneFattura.Attiva ? "receiver" : "sender";
        string? controparte = null;
        string? partitaIva = null;
        if (item.TryGetProperty(partyName, out var party) && party.ValueKind == JsonValueKind.Object)
        {
            controparte = ReadString(party, "description");
            partitaIva = ReadString(party, "vatCode");
        }

        return new FatturaRemota(id, idSdi, numero, data, controparte, partitaIva);
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private async Task<JsonDocument> SendJsonAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        ArubaCredentials creds,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(creds, cancellationToken);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Aruba ha risposto {(int)response.StatusCode} su {url}: {Truncate(body, 300)}");
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private async Task<string> GetTokenAsync(ArubaCredentials creds, CancellationToken cancellationToken)
    {
        await authLock.WaitAsync(cancellationToken);
        try
        {
            if (cachedToken is not null && cachedDemo == creds.UsaDemo && clock.UtcNow < tokenExpiresAt)
            {
                return cachedToken;
            }

            var (auth, _) = creds.UsaDemo ? Demo : Production;
            var client = httpFactory.CreateClient(HttpClientName);

            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = creds.Username,
                ["password"] = creds.Password,
            });

            using var response = await client.PostAsync($"{auth}/auth/signin", form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Login Aruba fallito ({(int)response.StatusCode}). Verifica le credenziali.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var token = ReadString(root, "access_token")
                ?? throw new InvalidOperationException("Risposta di login Aruba priva di access_token.");
            var expiresIn = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 1800;

            cachedToken = token;
            cachedDemo = creds.UsaDemo;
            tokenExpiresAt = clock.UtcNow.AddSeconds(Math.Max(60, expiresIn - 120));
            return token;
        }
        finally
        {
            authLock.Release();
        }
    }

    private async Task<ArubaCredentials?> ReadCredentialsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var cfg = await db.IntegrazioniAruba.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        if (cfg is null
            || !cfg.Abilitata
            || string.IsNullOrWhiteSpace(cfg.Username)
            || string.IsNullOrWhiteSpace(cfg.PasswordProtetta))
        {
            return null;
        }

        try
        {
            var password = protector.Unprotect(cfg.PasswordProtetta);
            return new ArubaCredentials(cfg.Username!, password, cfg.UsaDemo);
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Impossibile decifrare la password Aruba memorizzata.");
            return null;
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private sealed record ArubaCredentials(string Username, string Password, bool UsaDemo);
}
