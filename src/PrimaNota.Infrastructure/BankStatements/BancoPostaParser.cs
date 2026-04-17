using System.Globalization;
using System.Text.RegularExpressions;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.ContiFinanziari;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PrimaNota.Infrastructure.BankStatements;

/// <summary>
/// Parses a BancoPosta "Saldo e Movimenti" PDF into structured rows.
/// Strategy: uses PdfPig positional extraction only to separate the amounts zone
/// (right side) from the text zone (left side). The text zone is then split into
/// causale / operazione / descrizione using keyword matching — much more robust
/// than trying to align column boundaries from the header.
/// </summary>
public static class BancoPostaParser
{
    private static readonly CultureInfo It = new("it-IT");
    private static readonly Regex DatePattern = new(@"^\d{2}/\d{2}/\d{4}$", RegexOptions.Compiled);
    private static readonly Regex PeriodoPattern = new(
        @"PERIODO\s+DA\s+(\d{2}/\d{2}/\d{4})\s+A\s+(\d{2}/\d{2}/\d{4})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SaldoPattern = new(
        @"SALDO\s+CONTABILE:\s*([\d.,]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CausaleCodePattern = new(
        @"^(\d{1,4}[A-Z]*|[A-Z]{1,5}\d*[A-Z]*)\s+",
        RegexOptions.Compiled);

    private static readonly string[] OpKeywords =
    {
        "BONIFICO SEPA",
        "POSTAGIRO",
        "RICARICA POSTEPAY",
        "COMMISSIONI RICARICA POSTEPAY",
        "COMMISSIONI DOMICILIAZIONE (ADDEBITO DIRETTO SEPA)",
        "COMMISSIONI DOMICILIAZIONE",
        "F24 BPIOL",
        "ACCREDITO PER POSTEPAY CASHBACK BUSINESS",
        "TENUTA CONTO",
        "CANONE SERVIZIO COLLEGAMENTO TELEMATICO",
        "PAGAMENTO POS",
        "IMPOSTA DI BOLLO",
        "RATA FINANZIAMENTO",
        "MODELLO F24",
    };

    /// <summary>Parses the given PDF stream.</summary>
    /// <param name="pdfStream">Readable stream with the PDF content.</param>
    /// <returns>Parsed result.</returns>
    public static EstratoContoParseResult Parse(Stream pdfStream)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        using var document = PdfDocument.Open(pdfStream);

        var fullText = string.Join('\n', document.GetPages().Select(p => p.Text));
        var (periodoDa, periodoA) = ExtractPeriodo(fullText);
        var saldo = ExtractSaldo(fullText);

        var rows = new List<RigaEstrattoConto>();
        foreach (var page in document.GetPages())
        {
            rows.AddRange(ExtractRowsFromPage(page));
        }

        return new EstratoContoParseResult(periodoDa, periodoA, saldo, rows);
    }

    private static (DateOnly Da, DateOnly A) ExtractPeriodo(string text)
    {
        var m = PeriodoPattern.Match(text);
        return m.Success
            ? (ParseDate(m.Groups[1].Value), ParseDate(m.Groups[2].Value))
            : (DateOnly.MinValue, DateOnly.MinValue);
    }

    private static decimal? ExtractSaldo(string text)
    {
        var m = SaldoPattern.Match(text);
        return m.Success ? ParseAmount(m.Groups[1].Value) : null;
    }

    private static List<RigaEstrattoConto> ExtractRowsFromPage(Page page)
    {
        var words = page.GetWords().ToList();
        if (words.Count == 0)
        {
            return new List<RigaEstrattoConto>();
        }

        // Find the amounts zone boundary: the "Entrate" header word's left edge.
        var entrateWord = words.FirstOrDefault(w => w.Text == "Entrate");
        if (entrateWord is null)
        {
            return new List<RigaEstrattoConto>();
        }

        var amountsZoneLeft = entrateWord.BoundingBox.Left - 15;

        // Find header Y to exclude header line.
        var dataWord = words.FirstOrDefault(w => w.Text == "Data");
        var headerY = dataWord?.BoundingBox.Bottom ?? 0;

        // Split words into text zone (left) and amounts zone (right), excluding header/footer.
        var usciteWord = words.FirstOrDefault(w => w.Text == "Uscite");
        var usciteZoneLeft = usciteWord is not null ? usciteWord.BoundingBox.Left - 15 : amountsZoneLeft + 150;

        var dataWords = words
            .Where(w => !IsNearHeaderLine(w, headerY) && !IsFooter(w))
            .OrderByDescending(w => w.BoundingBox.Bottom)
            .ThenBy(w => w.BoundingBox.Left)
            .ToList();

        var lines = GroupIntoLines(dataWords, 3.0);

        var result = new List<RigaEstrattoConto>();
        RawRow? current = null;

        foreach (var line in lines)
        {
            if (line.Count >= 2 && DatePattern.IsMatch(line[0].Text) && DatePattern.IsMatch(line[1].Text))
            {
                if (current is not null)
                {
                    var parsed = BuildRiga(current, amountsZoneLeft, usciteZoneLeft);
                    if (parsed is not null)
                    {
                        result.Add(parsed);
                    }
                }

                current = new RawRow(line[0].Text, line[1].Text);
                foreach (var w in line.Skip(2))
                {
                    current.Words.Add(w);
                }
            }
            else if (current is not null)
            {
                foreach (var w in line)
                {
                    current.Words.Add(w);
                }
            }
        }

        if (current is not null)
        {
            var last = BuildRiga(current, amountsZoneLeft, usciteZoneLeft);
            if (last is not null)
            {
                result.Add(last);
            }
        }

        return result;
    }

    private static RigaEstrattoConto? BuildRiga(RawRow raw, double amountsLeft, double usciteLeft)
    {
        var dataContabile = TryParseDate(raw.DataContabile);
        var dataValuta = TryParseDate(raw.DataValuta);
        if (dataContabile is null || dataValuta is null)
        {
            return null;
        }

        var textParts = new List<string>();
        decimal? entrata = null;
        decimal? uscita = null;

        foreach (var w in raw.Words)
        {
            var x = w.BoundingBox.Left;

            if (x >= usciteLeft)
            {
                var amt = TryParseAmount(w.Text);
                if (amt.HasValue)
                {
                    uscita = (uscita ?? 0m) + amt.Value;
                }
            }
            else if (x >= amountsLeft)
            {
                var amt = TryParseAmount(w.Text);
                if (amt.HasValue)
                {
                    entrata = (entrata ?? 0m) + amt.Value;
                }
            }
            else if (!DatePattern.IsMatch(w.Text))
            {
                textParts.Add(w.Text);
            }
        }

        var importo = (entrata ?? 0m) - Math.Abs(uscita ?? 0m);
        if (importo == 0m && entrata is null && uscita is null)
        {
            return null;
        }

        var fullText = string.Join(' ', textParts);
        var (causale, operazione, descrizione) = SplitTextFields(fullText);

        return new RigaEstrattoConto(
            dataContabile.Value,
            dataValuta.Value,
            causale,
            operazione,
            descrizione,
            importo);
    }

    private static (string? Causale, string? Operazione, string? Descrizione) SplitTextFields(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null, null);
        }

        // Step 1: extract causale code from the beginning (e.g. "48", "26", "PO", "380PRIRIC", "1902I").
        string? causale = null;
        var causaleMatch = CausaleCodePattern.Match(text);
        if (causaleMatch.Success)
        {
            causale = causaleMatch.Groups[1].Value;
            text = text[causaleMatch.Length..];
        }

        // Step 2: match known operation keywords at the start of the remaining text.
        var opMatch = OpKeywords
            .OrderByDescending(k => k.Length)
            .FirstOrDefault(kw => text.StartsWith(kw, StringComparison.OrdinalIgnoreCase));

        string? operazione = null;
        if (opMatch is not null)
        {
            operazione = opMatch;
            text = text[opMatch.Length..].TrimStart();
        }

        var descrizione = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return (causale, operazione, descrizione);
    }

    private static List<List<Word>> GroupIntoLines(List<Word> words, double tolerance)
    {
        var lines = new List<List<Word>>();
        List<Word>? currentLine = null;
        double currentY = double.MinValue;

        foreach (var w in words)
        {
            if (currentLine is null || Math.Abs(w.BoundingBox.Bottom - currentY) > tolerance)
            {
                currentLine = new List<Word>();
                lines.Add(currentLine);
                currentY = w.BoundingBox.Bottom;
            }

            currentLine.Add(w);
        }

        return lines;
    }

    private static bool IsNearHeaderLine(Word w, double headerY) =>
        Math.Abs(w.BoundingBox.Bottom - headerY) < 6.0;

    private static bool IsFooter(Word w) =>
        w.Text is "Poste" or "Italiane" or "BancoPosta" or "Crescere" or "sostenibili."
        or "neutral" or "Patrimonio";

    private static DateOnly ParseDate(string s) => DateOnly.ParseExact(s, "dd/MM/yyyy", It);

    private static DateOnly? TryParseDate(string s) =>
        DateOnly.TryParseExact(s, "dd/MM/yyyy", It, DateTimeStyles.None, out var d) ? d : null;

    private static decimal ParseAmount(string s)
    {
        var clean = s.Replace(".", string.Empty).Replace(',', '.');
        return decimal.Parse(clean, CultureInfo.InvariantCulture);
    }

    private static decimal? TryParseAmount(string s)
    {
        var clean = s.Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(clean, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private sealed class RawRow
    {
        public RawRow(string dataContabile, string dataValuta)
        {
            DataContabile = dataContabile;
            DataValuta = dataValuta;
        }

        public string DataContabile { get; }

        public string DataValuta { get; }

        public List<Word> Words { get; } = new();
    }
}
