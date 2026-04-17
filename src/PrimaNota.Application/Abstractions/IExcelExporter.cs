using PrimaNota.Application.Anagrafiche;
using PrimaNota.Application.Iva;
using PrimaNota.Application.PrimaNota;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Application.Abstractions;

/// <summary>Generates Excel workbooks from domain data.</summary>
public interface IExcelExporter
{
    /// <summary>Exports the movements list.</summary>
    /// <param name="items">Movements.</param>
    /// <param name="anno">Fiscal year.</param>
    /// <returns>XLSX bytes.</returns>
    byte[] ExportMovimenti(IReadOnlyList<MovimentoListItemDto> items, int anno);

    /// <summary>Exports a VAT register.</summary>
    /// <param name="rows">Register rows.</param>
    /// <param name="registro">Register kind.</param>
    /// <param name="periodo">Period.</param>
    /// <returns>XLSX bytes.</returns>
    byte[] ExportRegistroIva(IReadOnlyList<RegistroIvaRigaDto> rows, TipoRegistroIva registro, IvaPeriodo periodo);

    /// <summary>Exports the VAT liquidation summary.</summary>
    /// <param name="dto">Liquidation data.</param>
    /// <param name="periodo">Period.</param>
    /// <returns>XLSX bytes.</returns>
    byte[] ExportLiquidazione(LiquidazioneIvaDto dto, IvaPeriodo periodo);

    /// <summary>Exports an account statement (scheda anagrafica).</summary>
    /// <param name="scheda">Scheda data.</param>
    /// <returns>XLSX bytes.</returns>
    byte[] ExportScheda(SchedaAnagraficaDto scheda);
}
