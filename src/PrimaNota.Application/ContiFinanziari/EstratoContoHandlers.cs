using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Application.ContiFinanziari;

/// <summary>Compact DTO for the import list.</summary>
public sealed record EstratoContoImportDto(
    Guid Id,
    Guid ContoFinanziarioId,
    string ContoNome,
    string NomeFile,
    DateOnly PeriodoDa,
    DateOnly PeriodoA,
    decimal? SaldoContabile,
    int TotaleRighe,
    int RigheDaRiconciliare,
    DateTimeOffset ImportedAt);

/// <summary>Row DTO for the detail view.</summary>
public sealed record RigaEstratoContoDto(
    Guid Id,
    DateOnly DataContabile,
    DateOnly DataValuta,
    string? CausaleOperazione,
    string? Operazione,
    string? Descrizione,
    decimal Importo,
    StatoRiconciliazione Stato,
    Guid? MovimentoId,
    Guid? PagamentoId);

/// <summary>Imports a bank statement file for a financial account.</summary>
/// <param name="ContoFinanziarioId">Target account.</param>
/// <param name="FileName">Original file name.</param>
/// <param name="Content">File stream (caller keeps ownership).</param>
/// <param name="ConnectorId">Explicit bank connector to use (null = auto-detect).</param>
public sealed record ImportEstrattoConto(
    Guid ContoFinanziarioId,
    string FileName,
    Stream Content,
    string? ConnectorId = null) : IRequest<Guid>;

/// <summary>Lists the bank connectors available for import.</summary>
public sealed record ListBankConnectors : IRequest<IReadOnlyList<BankConnectorInfo>>;

/// <summary>Lists all imports for a given account.</summary>
/// <param name="ContoFinanziarioId">Account filter (null = all).</param>
public sealed record ListEstrattiConto(Guid? ContoFinanziarioId = null) : IRequest<IReadOnlyList<EstratoContoImportDto>>;

/// <summary>Gets the rows of a specific import.</summary>
/// <param name="ImportId">Import identifier.</param>
public sealed record GetRigheEstrattoConto(Guid ImportId) : IRequest<IReadOnlyList<RigaEstratoContoDto>>;

/// <summary>Deletes an import and all its rows.</summary>
/// <param name="ImportId">Import identifier.</param>
public sealed record DeleteEstrattoConto(Guid ImportId) : IRequest;

/// <summary>Handler for <see cref="ImportEstrattoConto"/>.</summary>
public sealed class ImportEstratoContoHandler : IRequestHandler<ImportEstrattoConto, Guid>
{
    private readonly IApplicationDbContext db;
    private readonly IEstratoContoParser parser;

    /// <summary>Initializes a new instance of the <see cref="ImportEstratoContoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    /// <param name="parser">Bank-statement parser.</param>
    public ImportEstratoContoHandler(IApplicationDbContext db, IEstratoContoParser parser)
    {
        this.db = db;
        this.parser = parser;
    }

    /// <inheritdoc />
    public async Task<Guid> Handle(ImportEstrattoConto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conto = await db.ContiFinanziari.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContoFinanziarioId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conto finanziario {request.ContoFinanziarioId} non trovato.");

        var result = parser.Parse(request.Content, request.FileName, request.ConnectorId);

        var import = new EstratoContoImport(
            conto.Id,
            request.FileName,
            result.PeriodoDa,
            result.PeriodoA,
            result.SaldoContabile);

        foreach (var riga in result.Righe)
        {
            import.AddRiga(riga);
        }

        db.EstrattiConto.Add(import);
        await db.SaveChangesAsync(cancellationToken);
        return import.Id;
    }
}

/// <summary>Handler for <see cref="ListEstrattiConto"/>.</summary>
public sealed class ListEstrattiContoHandler : IRequestHandler<ListEstrattiConto, IReadOnlyList<EstratoContoImportDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListEstrattiContoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ListEstrattiContoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstratoContoImportDto>> Handle(ListEstrattiConto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var q = db.EstrattiConto.AsNoTracking()
            .Include(e => e.Righe)
            .AsQueryable();

        if (request.ContoFinanziarioId is { } contoId)
        {
            q = q.Where(e => e.ContoFinanziarioId == contoId);
        }

        var imports = await q
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var contiById = await db.ContiFinanziari.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Nome, cancellationToken);

        return imports.Select(e => new EstratoContoImportDto(
            e.Id,
            e.ContoFinanziarioId,
            contiById.GetValueOrDefault(e.ContoFinanziarioId, "?"),
            e.NomeFile,
            e.PeriodoDa,
            e.PeriodoA,
            e.SaldoContabile,
            e.Righe.Count,
            e.RigheDaRiconciliare,
            e.CreatedAt)).ToList();
    }
}

/// <summary>Handler for <see cref="GetRigheEstrattoConto"/>.</summary>
public sealed class GetRigheEstratoContoHandler : IRequestHandler<GetRigheEstrattoConto, IReadOnlyList<RigaEstratoContoDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetRigheEstratoContoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetRigheEstratoContoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RigaEstratoContoDto>> Handle(GetRigheEstrattoConto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var import = await db.EstrattiConto.AsNoTracking()
            .Include(e => e.Righe)
            .FirstOrDefaultAsync(e => e.Id == request.ImportId, cancellationToken);

        if (import is null)
        {
            return Array.Empty<RigaEstratoContoDto>();
        }

        return import.Righe
            .OrderByDescending(r => r.DataContabile)
            .Select(r => new RigaEstratoContoDto(
                r.Id,
                r.DataContabile,
                r.DataValuta,
                r.CausaleOperazione,
                r.Operazione,
                r.Descrizione,
                r.Importo,
                r.Stato,
                r.MovimentoId,
                r.PagamentoId))
            .ToList();
    }
}

/// <summary>Handler for <see cref="ListBankConnectors"/>.</summary>
public sealed class ListBankConnectorsHandler : IRequestHandler<ListBankConnectors, IReadOnlyList<BankConnectorInfo>>
{
    private readonly IEstratoContoParser parser;

    /// <summary>Initializes a new instance of the <see cref="ListBankConnectorsHandler"/> class.</summary>
    /// <param name="parser">Bank-statement parser.</param>
    public ListBankConnectorsHandler(IEstratoContoParser parser) => this.parser = parser;

    /// <inheritdoc />
    public Task<IReadOnlyList<BankConnectorInfo>> Handle(ListBankConnectors request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(parser.AvailableConnectors);
    }
}

/// <summary>Handler for <see cref="DeleteEstrattoConto"/>.</summary>
public sealed class DeleteEstratoContoHandler : IRequestHandler<DeleteEstrattoConto>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="DeleteEstratoContoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public DeleteEstratoContoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(DeleteEstrattoConto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var import = await db.EstrattiConto
            .FirstOrDefaultAsync(e => e.Id == request.ImportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Import {request.ImportId} non trovato.");

        db.EstrattiConto.Remove(import);
        await db.SaveChangesAsync(cancellationToken);
    }
}
