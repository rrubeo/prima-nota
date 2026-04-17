using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.Anagrafiche;
using PrimaNota.Application.Iva;
using PrimaNota.Application.PrimaNota;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Application.Reporting;

/// <summary>Exports the prima nota movements list as Excel.</summary>
/// <param name="Anno">Fiscal year.</param>
public sealed record ExportMovimentiExcel(int Anno) : IRequest<ExportResult>;

/// <summary>Exports a VAT register as Excel.</summary>
/// <param name="Registro">Register kind.</param>
/// <param name="Periodo">Period.</param>
public sealed record ExportRegistroIvaExcel(TipoRegistroIva Registro, IvaPeriodo Periodo) : IRequest<ExportResult>;

/// <summary>Exports the VAT liquidation as Excel.</summary>
/// <param name="Periodo">Period.</param>
public sealed record ExportLiquidazioneExcel(IvaPeriodo Periodo) : IRequest<ExportResult>;

/// <summary>Exports the scheda anagrafica as Excel.</summary>
/// <param name="AnagraficaId">Counterparty id.</param>
/// <param name="Anno">Optional year filter.</param>
public sealed record ExportSchedaAnagraficaExcel(Guid AnagraficaId, int? Anno) : IRequest<ExportResult>;

/// <summary>Exports a full "pacchetto commercialista" ZIP for a period.</summary>
/// <param name="Anno">Fiscal year.</param>
/// <param name="PeriodicitaIva">Periodicity for splitting IVA registers.</param>
public sealed record ExportPacchettoCommercialista(int Anno, PeriodicitaIva PeriodicitaIva) : IRequest<ExportResult>;

/// <summary>Handler for <see cref="ExportMovimentiExcel"/>.</summary>
public sealed class ExportMovimentiExcelHandler : IRequestHandler<ExportMovimentiExcel, ExportResult>
{
    private readonly IMediator mediator;
    private readonly IExcelExporter excel;

    /// <summary>Initializes a new instance of the <see cref="ExportMovimentiExcelHandler"/> class.</summary>
    /// <param name="mediator">Mediator.</param>
    /// <param name="excel">Excel exporter.</param>
    public ExportMovimentiExcelHandler(IMediator mediator, IExcelExporter excel)
    {
        this.mediator = mediator;
        this.excel = excel;
    }

    /// <inheritdoc />
    public async Task<ExportResult> Handle(ExportMovimentiExcel request, CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListMovimenti(request.Anno), cancellationToken);
        var bytes = excel.ExportMovimenti(items, request.Anno);
        return new ExportResult($"PrimaNota_{request.Anno}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }
}

/// <summary>Handler for <see cref="ExportRegistroIvaExcel"/>.</summary>
public sealed class ExportRegistroIvaExcelHandler : IRequestHandler<ExportRegistroIvaExcel, ExportResult>
{
    private readonly IMediator mediator;
    private readonly IExcelExporter excel;

    /// <summary>Initializes a new instance of the <see cref="ExportRegistroIvaExcelHandler"/> class.</summary>
    /// <param name="mediator">Mediator.</param>
    /// <param name="excel">Excel exporter.</param>
    public ExportRegistroIvaExcelHandler(IMediator mediator, IExcelExporter excel)
    {
        this.mediator = mediator;
        this.excel = excel;
    }

    /// <inheritdoc />
    public async Task<ExportResult> Handle(ExportRegistroIvaExcel request, CancellationToken cancellationToken)
    {
        var rows = await mediator.Send(new GetRegistroIva(request.Registro, request.Periodo), cancellationToken);
        var bytes = excel.ExportRegistroIva(rows, request.Registro, request.Periodo);
        return new ExportResult(
            $"RegistroIVA_{request.Registro}_{request.Periodo.Label}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            bytes);
    }
}

/// <summary>Handler for <see cref="ExportLiquidazioneExcel"/>.</summary>
public sealed class ExportLiquidazioneExcelHandler : IRequestHandler<ExportLiquidazioneExcel, ExportResult>
{
    private readonly IMediator mediator;
    private readonly IExcelExporter excel;

    /// <summary>Initializes a new instance of the <see cref="ExportLiquidazioneExcelHandler"/> class.</summary>
    /// <param name="mediator">Mediator.</param>
    /// <param name="excel">Excel exporter.</param>
    public ExportLiquidazioneExcelHandler(IMediator mediator, IExcelExporter excel)
    {
        this.mediator = mediator;
        this.excel = excel;
    }

    /// <inheritdoc />
    public async Task<ExportResult> Handle(ExportLiquidazioneExcel request, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetLiquidazioneIva(request.Periodo), cancellationToken);
        var bytes = excel.ExportLiquidazione(dto, request.Periodo);
        return new ExportResult(
            $"LiquidazioneIVA_{request.Periodo.Label}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            bytes);
    }
}

/// <summary>Handler for <see cref="ExportSchedaAnagraficaExcel"/>.</summary>
public sealed class ExportSchedaAnagraficaExcelHandler : IRequestHandler<ExportSchedaAnagraficaExcel, ExportResult>
{
    private readonly IMediator mediator;
    private readonly IExcelExporter excel;

    /// <summary>Initializes a new instance of the <see cref="ExportSchedaAnagraficaExcelHandler"/> class.</summary>
    /// <param name="mediator">Mediator.</param>
    /// <param name="excel">Excel exporter.</param>
    public ExportSchedaAnagraficaExcelHandler(IMediator mediator, IExcelExporter excel)
    {
        this.mediator = mediator;
        this.excel = excel;
    }

    /// <inheritdoc />
    public async Task<ExportResult> Handle(ExportSchedaAnagraficaExcel request, CancellationToken cancellationToken)
    {
        var scheda = await mediator.Send(new GetSchedaAnagrafica(request.AnagraficaId, request.Anno), cancellationToken);
        if (scheda is null)
        {
            return new ExportResult("vuoto.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Array.Empty<byte>());
        }

        var bytes = excel.ExportScheda(scheda);
        return new ExportResult(
            $"Scheda_{scheda.RagioneSociale}_{request.Anno}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            bytes);
    }
}

/// <summary>Handler for <see cref="ExportPacchettoCommercialista"/>.</summary>
public sealed class ExportPacchettoCommercialistaHandler : IRequestHandler<ExportPacchettoCommercialista, ExportResult>
{
    private readonly IMediator mediator;

    /// <summary>Initializes a new instance of the <see cref="ExportPacchettoCommercialistaHandler"/> class.</summary>
    /// <param name="mediator">Mediator.</param>
    public ExportPacchettoCommercialistaHandler(IMediator mediator) => this.mediator = mediator;

    /// <inheritdoc />
    public async Task<ExportResult> Handle(ExportPacchettoCommercialista request, CancellationToken cancellationToken)
    {
        var files = new List<(string Name, byte[] Content)>();

        var movimenti = await mediator.Send(new ExportMovimentiExcel(request.Anno), cancellationToken);
        files.Add((movimenti.FileName, movimenti.Content));

        var periodi = IvaPeriodo.Elenco(request.Anno, request.PeriodicitaIva);
        foreach (var p in periodi)
        {
            var vendite = await mediator.Send(new ExportRegistroIvaExcel(TipoRegistroIva.Vendite, p), cancellationToken);
            files.Add((vendite.FileName, vendite.Content));

            var acquisti = await mediator.Send(new ExportRegistroIvaExcel(TipoRegistroIva.Acquisti, p), cancellationToken);
            files.Add((acquisti.FileName, acquisti.Content));

            var liq = await mediator.Send(new ExportLiquidazioneExcel(p), cancellationToken);
            files.Add((liq.FileName, liq.Content));
        }

        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            foreach (var (name, content) in files)
            {
                if (content.Length == 0)
                {
                    continue;
                }

                var entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);

#pragma warning disable S6966 // ZipArchiveEntry.Open() has no async overload
                using var entryStream = entry.Open();
                entryStream.Write(content);
#pragma warning restore S6966
            }
        }

        return new ExportResult(
            $"Commercialista_{request.Anno}.zip",
            "application/zip",
            ms.ToArray());
    }
}
