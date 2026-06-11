using System.Globalization;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Infrastructure.BankStatements;

/// <summary>
/// Connector for the Poste Italiane "BancoPosta — Saldo e Movimenti" CSV export.
/// The export is a multi-section delimited file (tab or semicolon): a <c>RIEPILOGO</c> block
/// with the balance, a <c>FILTRI DI RICERCA</c> block with the period, and a
/// <c>LISTA MOVIMENTI</c> block with one row per movement.
/// Columns are mapped by header name, so the connector is resilient to column reordering.
/// </summary>
public sealed class BancoPostaCsvConnector : IBankStatementConnector
{
    private static readonly CultureInfo It = new("it-IT");
    private static readonly char[] CandidateDelimiters = { '\t', ';' };

    /// <inheritdoc />
    public string Id => "bancoposta-csv";

    /// <inheritdoc />
    public string DisplayName => "Poste Italiane — BancoPosta (CSV)";

    /// <inheritdoc />
    public bool CanParse(string fileName, string content)
    {
        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // The RIEPILOGO header reliably mentions "BancoPosta"; the movements block has
        // a distinctive "Descrizione movimento" column header.
        return content.Contains("BancoPosta", StringComparison.OrdinalIgnoreCase)
            && content.Contains("Descrizione movimento", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public EstratoContoParseResult Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var delimiter = DetectDelimiter(lines);

        var righe = ParseMovements(lines, delimiter);
        var (periodoDa, periodoA) = ExtractPeriodo(lines, delimiter, righe);
        var saldo = ExtractSaldo(lines, delimiter);

        return new EstratoContoParseResult(periodoDa, periodoA, saldo, righe);
    }

    private static char DetectDelimiter(string[] lines)
    {
        // Choose the delimiter that appears in the movements header line.
        var header = lines.FirstOrDefault(l =>
            l.Contains("Data contabile", StringComparison.OrdinalIgnoreCase)
            && l.Contains("Data valuta", StringComparison.OrdinalIgnoreCase));

        if (header is not null)
        {
            return CandidateDelimiters.OrderByDescending(d => header.Count(c => c == d)).First();
        }

        // Fallback: whichever candidate is most frequent overall.
        return CandidateDelimiters
            .OrderByDescending(d => lines.Sum(l => l.Count(c => c == d)))
            .First();
    }

    private static List<RigaEstrattoConto> ParseMovements(string[] lines, char delimiter)
    {
        var headerIndex = Array.FindIndex(lines, l =>
            l.Contains("Data contabile", StringComparison.OrdinalIgnoreCase)
            && l.Contains("Data valuta", StringComparison.OrdinalIgnoreCase));

        var righe = new List<RigaEstrattoConto>();
        if (headerIndex < 0)
        {
            return righe;
        }

        var header = SplitLine(lines[headerIndex], delimiter);
        var idxDataContabile = IndexOfHeader(header, "Data contabile");
        var idxDataValuta = IndexOfHeader(header, "Data valuta");
        var idxAddebito = IndexOfHeader(header, "Addebito");
        var idxAccredito = IndexOfHeader(header, "Accredito");
        var idxCausale = IndexOfHeader(header, "Causale operazione");
        var idxOperazione = IndexOfHeader(header, "Operazione");
        var idxDescrizione = IndexOfHeader(header, "Descrizione movimento");

        for (var i = headerIndex + 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                break;
            }

            var cells = SplitLine(lines[i], delimiter);

            var dataContabile = TryParseDate(Cell(cells, idxDataContabile));
            var dataValuta = TryParseDate(Cell(cells, idxDataValuta)) ?? dataContabile;
            if (dataContabile is null)
            {
                continue;
            }

            var accredito = TryParseAmount(Cell(cells, idxAccredito));
            var addebito = TryParseAmount(Cell(cells, idxAddebito));
            if (accredito is null && addebito is null)
            {
                continue;
            }

            // Accredito is positive; addebito already carries its minus sign in the export.
            var importo = accredito ?? addebito ?? 0m;

            righe.Add(new RigaEstrattoConto(
                dataContabile.Value,
                dataValuta!.Value,
                NullIfEmpty(Cell(cells, idxCausale)),
                NullIfEmpty(Cell(cells, idxDescrizione)),
                NullIfEmpty(Cell(cells, idxOperazione)),
                importo));
        }

        return righe;
    }

    private static (DateOnly Da, DateOnly A) ExtractPeriodo(
        string[] lines,
        char delimiter,
        List<RigaEstrattoConto> righe)
    {
        var headerIndex = Array.FindIndex(lines, l =>
            l.Contains("Data inizio", StringComparison.OrdinalIgnoreCase)
            && l.Contains("Data fine", StringComparison.OrdinalIgnoreCase));

        if (headerIndex >= 0 && headerIndex + 1 < lines.Length)
        {
            var header = SplitLine(lines[headerIndex], delimiter);
            var values = SplitLine(lines[headerIndex + 1], delimiter);
            var da = TryParseDate(Cell(values, IndexOfHeader(header, "Data inizio")));
            var a = TryParseDate(Cell(values, IndexOfHeader(header, "Data fine")));
            if (da is not null && a is not null)
            {
                return (da.Value, a.Value);
            }
        }

        // Fallback: span of the movement accounting dates.
        if (righe.Count > 0)
        {
            return (righe.Min(r => r.DataContabile), righe.Max(r => r.DataContabile));
        }

        return (DateOnly.MinValue, DateOnly.MinValue);
    }

    private static decimal? ExtractSaldo(string[] lines, char delimiter)
    {
        var headerIndex = Array.FindIndex(lines, l =>
            l.Contains("Saldo contabile conto", StringComparison.OrdinalIgnoreCase));

        if (headerIndex < 0 || headerIndex + 1 >= lines.Length)
        {
            return null;
        }

        var header = SplitLine(lines[headerIndex], delimiter);
        var values = SplitLine(lines[headerIndex + 1], delimiter);
        return TryParseAmount(Cell(values, IndexOfHeader(header, "Saldo contabile conto")));
    }

    private static string[] SplitLine(string line, char delimiter) => line.Split(delimiter);

    private static int IndexOfHeader(string[] header, string name) =>
        Array.FindIndex(header, h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string Cell(string[] cells, int index) =>
        index >= 0 && index < cells.Length ? cells[index].Trim() : string.Empty;

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static DateOnly? TryParseDate(string value) =>
        DateOnly.TryParseExact(value, "dd/MM/yyyy", It, DateTimeStyles.None, out var d) ? d : null;

    private static decimal? TryParseAmount(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var clean = value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        return decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v)
            ? v
            : null;
    }
}
