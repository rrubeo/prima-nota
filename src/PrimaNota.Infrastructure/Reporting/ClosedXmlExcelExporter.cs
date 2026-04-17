using ClosedXML.Excel;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.Anagrafiche;
using PrimaNota.Application.Iva;
using PrimaNota.Application.PrimaNota;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Infrastructure.Reporting;

/// <summary><see cref="IExcelExporter"/> implementation using ClosedXML.</summary>
public sealed class ClosedXmlExcelExporter : IExcelExporter
{
    /// <inheritdoc />
    public byte[] ExportMovimenti(IReadOnlyList<MovimentoListItemDto> items, int anno)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet($"PrimaNota {anno}");

        var headers = new[] { "Data", "Causale", "Descrizione", "Numero", "Anagrafica", "Totale", "Righe", "Stato", "Residuo", "Saldato" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        StyleHeader(ws, 1, headers.Length);

        for (var r = 0; r < items.Count; r++)
        {
            var m = items[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = m.Data.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            ws.Cell(row, 2).Value = $"{m.CausaleCodice} - {m.CausaleNome}";
            ws.Cell(row, 3).Value = m.Descrizione;
            ws.Cell(row, 4).Value = m.Numero;
            ws.Cell(row, 5).Value = m.AnagraficaRagioneSociale;
            ws.Cell(row, 6).Value = m.Totale;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = m.NumeroRighe;
            ws.Cell(row, 8).Value = m.Stato.ToString();
            ws.Cell(row, 9).Value = m.Residuo;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = m.IsFullyPaid ? "Si" : "No";
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    /// <inheritdoc />
    public byte[] ExportRegistroIva(IReadOnlyList<RegistroIvaRigaDto> rows, TipoRegistroIva registro, IvaPeriodo periodo)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet($"Registro {registro}");

        var headers = new[] { "Data", "Numero", "Causale", "Controparte", "P.IVA/CF", "Descrizione", "Aliquota", "%", "Imponibile", "Imposta", "Totale" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        StyleHeader(ws, 1, headers.Length);

        for (var r = 0; r < rows.Count; r++)
        {
            var d = rows[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = d.Data.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            ws.Cell(row, 2).Value = d.Numero;
            ws.Cell(row, 3).Value = d.CausaleCodice;
            ws.Cell(row, 4).Value = d.AnagraficaRagioneSociale;
            ws.Cell(row, 5).Value = d.PartitaIva ?? d.CodiceFiscale;
            ws.Cell(row, 6).Value = d.Descrizione;
            ws.Cell(row, 7).Value = d.AliquotaCodice;
            ws.Cell(row, 8).Value = d.AliquotaPercentuale;
            ws.Cell(row, 9).Value = d.Imponibile;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = d.Imposta;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 11).Value = d.Lordo;
            ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";
        }

        var totRow = rows.Count + 2;
        ws.Cell(totRow, 8).Value = "Totali";
        ws.Cell(totRow, 8).Style.Font.Bold = true;
        ws.Cell(totRow, 9).Value = rows.Sum(r => r.Imponibile);
        ws.Cell(totRow, 9).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(totRow, 9).Style.Font.Bold = true;
        ws.Cell(totRow, 10).Value = rows.Sum(r => r.Imposta);
        ws.Cell(totRow, 10).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(totRow, 10).Style.Font.Bold = true;
        ws.Cell(totRow, 11).Value = rows.Sum(r => r.Lordo);
        ws.Cell(totRow, 11).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(totRow, 11).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    /// <inheritdoc />
    public byte[] ExportLiquidazione(LiquidazioneIvaDto dto, IvaPeriodo periodo)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet($"Liquidazione {periodo.Label}");

        ws.Cell(1, 1).Value = "Liquidazione IVA";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = "Periodo";
        ws.Cell(2, 2).Value = periodo.Label;
        ws.Cell(3, 1).Value = "Regime";
        ws.Cell(3, 2).Value = dto.Regime.ToString();
        ws.Cell(4, 1).Value = "Esigibilita";
        ws.Cell(4, 2).Value = dto.Esigibilita.ToString();

        ws.Cell(6, 1).Value = "Voce";
        ws.Cell(6, 2).Value = "Importo";
        ws.Cell(6, 1).Style.Font.Bold = true;
        ws.Cell(6, 2).Style.Font.Bold = true;

        var labels = new[]
        {
            ("IVA a debito", dto.IvaDebito),
            ("IVA a credito totale", dto.IvaCreditoTotale),
            ("IVA indetraibile", dto.IvaCreditoIndetraibile),
            ("IVA a credito detraibile", dto.IvaCreditoDetraibile),
            ("Saldo periodo", dto.SaldoPeriodo),
            ("Credito riportato", dto.CreditoRiportato),
            ("Saldo finale", dto.SaldoFinale),
        };

        for (var i = 0; i < labels.Length; i++)
        {
            ws.Cell(7 + i, 1).Value = labels[i].Item1;
            ws.Cell(7 + i, 2).Value = labels[i].Item2;
            ws.Cell(7 + i, 2).Style.NumberFormat.Format = "#,##0.00";
        }

        ws.Cell(7 + labels.Length - 1, 1).Style.Font.Bold = true;
        ws.Cell(7 + labels.Length - 1, 2).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    /// <inheritdoc />
    public byte[] ExportScheda(SchedaAnagraficaDto scheda)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Scheda");

        ws.Cell(1, 1).Value = "Scheda conto";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = scheda.RagioneSociale;
        ws.Cell(3, 1).Value = "Dare totale";
        ws.Cell(3, 2).Value = scheda.Dare;
        ws.Cell(3, 2).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(4, 1).Value = "Avere totale";
        ws.Cell(4, 2).Value = scheda.Avere;
        ws.Cell(4, 2).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(5, 1).Value = "Saldo";
        ws.Cell(5, 2).Value = scheda.SaldoFinale;
        ws.Cell(5, 2).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(5, 1).Style.Font.Bold = true;
        ws.Cell(5, 2).Style.Font.Bold = true;

        var headers = new[] { "Data", "Tipo", "Descrizione", "Numero", "Dare", "Avere", "Saldo" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(7, i + 1).Value = headers[i];
        }

        StyleHeader(ws, 7, headers.Length);

        for (var r = 0; r < scheda.Righe.Count; r++)
        {
            var d = scheda.Righe[r];
            var row = r + 8;
            ws.Cell(row, 1).Value = d.Data.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            ws.Cell(row, 2).Value = d.Tipo.ToString();
            ws.Cell(row, 3).Value = d.Descrizione;
            ws.Cell(row, 4).Value = d.Numero;
            ws.Cell(row, 5).Value = d.Dare;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = d.Avere;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = d.Saldo;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    private static void StyleHeader(IXLWorksheet ws, int row, int cols)
    {
        for (var i = 1; i <= cols; i++)
        {
            ws.Cell(row, i).Style.Font.Bold = true;
            ws.Cell(row, i).Style.Fill.BackgroundColor = XLColor.FromHtml("#f3f4f6");
        }
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
