using System.Globalization;
using System.Xml.Linq;

namespace PrimaNota.Application.PrimaNota.Import;

/// <summary>
/// Parses an Italian electronic-invoice XML (FatturaPA / FatturaElettronica FPR12, FPA12,
/// FSM10) into a minimal DTO suitable for movement creation. Tolerant to the two common
/// layouts: with and without the <c>http://ivaservizi.agenziaentrate.gov.it/.../v1.2</c>
/// namespace on the children of <c>FatturaElettronicaHeader</c> / <c>FatturaElettronicaBody</c>.
/// </summary>
public static class FatturaElettronicaParser
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Parses a fattura elettronica from a stream.</summary>
    /// <param name="stream">Open, readable XML stream.</param>
    /// <returns>Parsed DTO.</returns>
    /// <exception cref="FatturaElettronicaParseException">If the structure is not recognised.</exception>
    public static FatturaElettronicaDto Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        XDocument doc;
        try
        {
            doc = XDocument.Load(stream, LoadOptions.None);
        }
        catch (Exception ex)
        {
            throw new FatturaElettronicaParseException("File XML non valido.", ex);
        }

        var root = doc.Root
            ?? throw new FatturaElettronicaParseException("Documento XML vuoto.");

        if (!string.Equals(root.Name.LocalName, "FatturaElettronica", StringComparison.Ordinal))
        {
            throw new FatturaElettronicaParseException(
                $"Root inatteso '{root.Name.LocalName}'. Atteso 'FatturaElettronica'.");
        }

        var header = FindChild(root, "FatturaElettronicaHeader")
            ?? throw new FatturaElettronicaParseException("Sezione <FatturaElettronicaHeader> mancante.");
        var body = FindChild(root, "FatturaElettronicaBody")
            ?? throw new FatturaElettronicaParseException("Sezione <FatturaElettronicaBody> mancante.");

        var cedenteEl = FindChild(header, "CedentePrestatore")
            ?? throw new FatturaElettronicaParseException("CedentePrestatore mancante.");
        var cessionarioEl = FindChild(header, "CessionarioCommittente")
            ?? throw new FatturaElettronicaParseException("CessionarioCommittente mancante.");

        var cedente = ParseSoggetto(cedenteEl);
        var cessionario = ParseSoggetto(cessionarioEl);

        var datiGenerali = FindChild(body, "DatiGenerali")
            ?? throw new FatturaElettronicaParseException("DatiGenerali mancante.");
        var datiGeneraliDoc = FindChild(datiGenerali, "DatiGeneraliDocumento")
            ?? throw new FatturaElettronicaParseException("DatiGeneraliDocumento mancante.");

        var dataStr = TextOf(datiGeneraliDoc, "Data")
            ?? throw new FatturaElettronicaParseException("Campo <Data> obbligatorio.");
        if (!DateOnly.TryParse(dataStr, Inv, DateTimeStyles.None, out var data))
        {
            throw new FatturaElettronicaParseException($"Data '{dataStr}' non riconosciuta (atteso ISO).");
        }

        var numero = TextOf(datiGeneraliDoc, "Numero");
        var divisa = TextOf(datiGeneraliDoc, "Divisa") ?? "EUR";
        var importoTotale = ParseDecimal(TextOf(datiGeneraliDoc, "ImportoTotaleDocumento")) ?? 0m;

        var datiBeni = FindChild(body, "DatiBeniServizi")
            ?? throw new FatturaElettronicaParseException("DatiBeniServizi mancante.");

        var riepilogo = FindChildren(datiBeni, "DatiRiepilogo")
            .Select(ParseRiepilogo)
            .ToList();

        if (riepilogo.Count == 0)
        {
            throw new FatturaElettronicaParseException("Nessun <DatiRiepilogo> presente.");
        }

        return new FatturaElettronicaDto(
            cedente,
            cessionario,
            data,
            string.IsNullOrWhiteSpace(numero) ? null : numero.Trim(),
            importoTotale,
            divisa,
            riepilogo);
    }

    private static FatturaSoggettoDto ParseSoggetto(XElement soggetto)
    {
        var datiAna = FindChild(soggetto, "DatiAnagrafici");
        var anagrafica = datiAna is null ? null : FindChild(datiAna, "Anagrafica");
        var idFiscaleIva = datiAna is null ? null : FindChild(datiAna, "IdFiscaleIVA");
        var codiceFiscale = datiAna is null ? null : TextOf(datiAna, "CodiceFiscale");

        var paese = idFiscaleIva is null ? null : TextOf(idFiscaleIva, "IdPaese");
        var partitaIva = idFiscaleIva is null ? null : TextOf(idFiscaleIva, "IdCodice");

        string? denominazione = null, nome = null, cognome = null;
        if (anagrafica is not null)
        {
            denominazione = TextOf(anagrafica, "Denominazione");
            nome = TextOf(anagrafica, "Nome");
            cognome = TextOf(anagrafica, "Cognome");
        }

        var sede = FindChild(soggetto, "Sede");
        var indirizzo = sede is null ? null : TextOf(sede, "Indirizzo");
        var cap = sede is null ? null : TextOf(sede, "CAP");
        var comune = sede is null ? null : TextOf(sede, "Comune");
        var provincia = sede is null ? null : TextOf(sede, "Provincia");
        var nazione = sede is null ? null : TextOf(sede, "Nazione");

        var contatti = FindChild(soggetto, "Contatti");
        var email = contatti is null ? null : TextOf(contatti, "Email");

        return new FatturaSoggettoDto(
            denominazione,
            nome,
            cognome,
            partitaIva,
            codiceFiscale,
            paese,
            indirizzo,
            cap,
            comune,
            provincia,
            nazione,
            email);
    }

    private static FatturaRiepilogoDto ParseRiepilogo(XElement r)
    {
        var aliquota = ParseDecimal(TextOf(r, "AliquotaIVA")) ?? 0m;
        var natura = TextOf(r, "Natura");
        var imponibile = ParseDecimal(TextOf(r, "ImponibileImporto")) ?? 0m;
        var imposta = ParseDecimal(TextOf(r, "Imposta")) ?? 0m;
        var rif = TextOf(r, "RiferimentoNormativo");
        return new FatturaRiepilogoDto(aliquota, natura, imponibile, imposta, rif);
    }

    private static XElement? FindChild(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    private static IEnumerable<XElement> FindChildren(XElement parent, string localName) =>
        parent.Elements().Where(e => e.Name.LocalName == localName);

    private static string? TextOf(XElement parent, string localName)
    {
        var child = FindChild(parent, localName);
        return child is null || string.IsNullOrWhiteSpace(child.Value) ? null : child.Value.Trim();
    }

    private static decimal? ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return decimal.TryParse(text, NumberStyles.Number, Inv, out var v) ? v : null;
    }
}

/// <summary>Raised when the XML does not conform to the expected FatturaElettronica shape.</summary>
public sealed class FatturaElettronicaParseException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="FatturaElettronicaParseException"/> class.</summary>
    /// <param name="message">Human-readable explanation.</param>
    public FatturaElettronicaParseException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FatturaElettronicaParseException"/> class with an inner exception.</summary>
    /// <param name="message">Human-readable explanation.</param>
    /// <param name="inner">Inner exception.</param>
    public FatturaElettronicaParseException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
