using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.ContiFinanziari;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.ContiFinanziari;

/// <summary>Candidate match between a bank row and a movement.</summary>
/// <param name="RigaId">Bank-statement row id.</param>
/// <param name="RigaDataContabile">Row accounting date.</param>
/// <param name="RigaImporto">Row signed amount.</param>
/// <param name="RigaDescrizione">Row description (for display).</param>
/// <param name="MovimentoId">Candidate movement id.</param>
/// <param name="MovimentoData">Movement date.</param>
/// <param name="MovimentoDescrizione">Movement description.</param>
/// <param name="MovimentoTotale">Movement total (signed).</param>
/// <param name="MovimentoNumero">Movement reference number.</param>
/// <param name="MovimentoAnagrafica">Counterparty name.</param>
/// <param name="Confidence">Match confidence: 1 = exact amount+date, 0.5 = exact amount only.</param>
public sealed record MatchCandidateDto(
    Guid RigaId,
    DateOnly RigaDataContabile,
    decimal RigaImporto,
    string? RigaDescrizione,
    Guid MovimentoId,
    DateOnly MovimentoData,
    string MovimentoDescrizione,
    decimal MovimentoTotale,
    string? MovimentoNumero,
    string? MovimentoAnagrafica,
    decimal Confidence);

/// <summary>Finds auto-match candidates for unreconciled rows of an import.</summary>
/// <param name="ImportId">Import id.</param>
/// <param name="ContoFinanziarioId">Financial account.</param>
public sealed record FindMatchCandidates(Guid ImportId, Guid ContoFinanziarioId)
    : IRequest<IReadOnlyList<MatchCandidateDto>>;

/// <summary>Manually reconciles a bank row to a movement, optionally creating a PagamentoMovimento.</summary>
/// <param name="ImportId">Import id.</param>
/// <param name="RigaId">Bank-statement row id.</param>
/// <param name="MovimentoId">Movement to link.</param>
/// <param name="CreaPagamento">If true, creates a PagamentoMovimento on the movement.</param>
public sealed record RiconciliaRiga(Guid ImportId, Guid RigaId, Guid MovimentoId, bool CreaPagamento = true)
    : IRequest;

/// <summary>Marks a bank row as excluded from reconciliation.</summary>
/// <param name="ImportId">Import id.</param>
/// <param name="RigaId">Row id.</param>
public sealed record EscludiRiga(Guid ImportId, Guid RigaId) : IRequest;

/// <summary>Resets a bank row back to "da riconciliare".</summary>
/// <param name="ImportId">Import id.</param>
/// <param name="RigaId">Row id.</param>
public sealed record ResetRiconciliazione(Guid ImportId, Guid RigaId) : IRequest;

/// <summary>Creates a new prima-nota movement from a bank-statement row and reconciles it.</summary>
/// <param name="ImportId">Import id.</param>
/// <param name="RigaId">Bank-statement row id.</param>
/// <param name="CausaleId">Causale to assign to the new movement.</param>
/// <param name="CategoriaId">Category for the line.</param>
/// <param name="ContoFinanziarioId">Financial account for the line.</param>
/// <param name="AnagraficaId">Optional counterparty.</param>
/// <param name="AliquotaIvaId">Optional VAT rate.</param>
/// <param name="ContoDestinazioneId">For giroconti: the second account that receives the opposite amount.</param>
public sealed record GeneraMovimentoDaRiga(
    Guid ImportId,
    Guid RigaId,
    Guid CausaleId,
    Guid CategoriaId,
    Guid ContoFinanziarioId,
    Guid? AnagraficaId,
    Guid? AliquotaIvaId,
    Guid? ContoDestinazioneId = null) : IRequest<Guid>;

/// <summary>Handler for <see cref="FindMatchCandidates"/>.</summary>
public sealed class FindMatchCandidatesHandler : IRequestHandler<FindMatchCandidates, IReadOnlyList<MatchCandidateDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="FindMatchCandidatesHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public FindMatchCandidatesHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MatchCandidateDto>> Handle(FindMatchCandidates request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var import = await db.EstrattiConto.AsNoTracking()
            .Include(e => e.Righe)
            .FirstOrDefaultAsync(e => e.Id == request.ImportId, cancellationToken);

        if (import is null)
        {
            return Array.Empty<MatchCandidateDto>();
        }

        var pendingRows = import.Righe
            .Where(r => r.Stato == StatoRiconciliazione.DaRiconciliare)
            .ToList();

        if (pendingRows.Count == 0)
        {
            return Array.Empty<MatchCandidateDto>();
        }

        var dateMin = pendingRows.Min(r => r.DataContabile).AddDays(-10);
        var dateMax = pendingRows.Max(r => r.DataContabile).AddDays(10);

        var movimenti = await (
            from m in db.Movimenti.AsNoTracking()
            where (m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled)
                  && m.Data >= dateMin && m.Data <= dateMax
                  && m.Righe.Any(r => r.ContoFinanziarioId == request.ContoFinanziarioId)
            join a in db.Anagrafiche.AsNoTracking() on m.AnagraficaId equals a.Id into aj
            from anag in aj.DefaultIfEmpty()
            select new
            {
                m.Id,
                m.Data,
                m.Descrizione,
                m.Numero,
                Totale = m.Righe.Sum(r => r.Importo),
                Anagrafica = anag != null ? anag.RagioneSociale : null,
            }).ToListAsync(cancellationToken);

        var candidates = new List<MatchCandidateDto>();

        foreach (var row in pendingRows)
        {
            foreach (var mov in movimenti)
            {
                var amountMatch = Math.Abs(row.Importo - mov.Totale) < 0.01m;
                if (!amountMatch)
                {
                    continue;
                }

                var daysDiff = Math.Abs(row.DataContabile.DayNumber - mov.Data.DayNumber);
                decimal confidence;
                if (daysDiff <= 2)
                {
                    confidence = 1.0m;
                }
                else if (daysDiff <= 7)
                {
                    confidence = 0.8m;
                }
                else
                {
                    confidence = 0.5m;
                }

                candidates.Add(new MatchCandidateDto(
                    row.Id,
                    row.DataContabile,
                    row.Importo,
                    row.Descrizione,
                    mov.Id,
                    mov.Data,
                    mov.Descrizione,
                    mov.Totale,
                    mov.Numero,
                    mov.Anagrafica,
                    confidence));
            }
        }

        return candidates
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => c.RigaDataContabile)
            .ToList();
    }
}

/// <summary>Handler for <see cref="RiconciliaRiga"/>.</summary>
public sealed class RiconciliaRigaHandler : IRequestHandler<RiconciliaRiga>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="RiconciliaRigaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public RiconciliaRigaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(RiconciliaRiga request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var import = await db.EstrattiConto
            .Include(e => e.Righe)
            .FirstOrDefaultAsync(e => e.Id == request.ImportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Import {request.ImportId} non trovato.");

        var riga = import.Righe.FirstOrDefault(r => r.Id == request.RigaId)
            ?? throw new KeyNotFoundException($"Riga {request.RigaId} non trovata.");

        Guid? pagamentoId = null;

        if (request.CreaPagamento && riga.Importo != 0m)
        {
            var movimento = await db.Movimenti
                .Include(m => m.Pagamenti)
                .Include(m => m.Righe)
                .FirstOrDefaultAsync(m => m.Id == request.MovimentoId, cancellationToken)
                ?? throw new KeyNotFoundException($"Movimento {request.MovimentoId} non trovato.");

            var importoPagamento = Math.Abs(riga.Importo);

            // Only create a PagamentoMovimento if the movement has enough residuo.
            // Cash sales or already-fully-paid invoices may have residuo = 0.
            if (movimento.TotaleLordo > 0m && importoPagamento <= movimento.Residuo + MovimentoPrimaNota.PagamentoTolerance)
            {
                var effettivo = Math.Min(importoPagamento, movimento.Residuo);
                if (effettivo > 0m)
                {
                    var pagamento = new PagamentoMovimento(
                        riga.DataContabile,
                        effettivo,
                        import.ContoFinanziarioId,
                        $"Riconciliazione estratto conto {riga.DataContabile:dd/MM/yyyy}");

                    movimento.AddPagamento(pagamento);
                    pagamentoId = pagamento.Id;
                }
            }
        }

        riga.Riconcilia(request.MovimentoId, pagamentoId);

        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="EscludiRiga"/>.</summary>
public sealed class EscludiRigaHandler : IRequestHandler<EscludiRiga>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="EscludiRigaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public EscludiRigaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(EscludiRiga request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var import = await db.EstrattiConto
            .Include(e => e.Righe)
            .FirstOrDefaultAsync(e => e.Id == request.ImportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Import {request.ImportId} non trovato.");

        var riga = import.Righe.FirstOrDefault(r => r.Id == request.RigaId)
            ?? throw new KeyNotFoundException($"Riga {request.RigaId} non trovata.");

        riga.Escludi();
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="ResetRiconciliazione"/>.</summary>
public sealed class ResetRiconciliazioneHandler : IRequestHandler<ResetRiconciliazione>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ResetRiconciliazioneHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ResetRiconciliazioneHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(ResetRiconciliazione request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var import = await db.EstrattiConto
            .Include(e => e.Righe)
            .FirstOrDefaultAsync(e => e.Id == request.ImportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Import {request.ImportId} non trovato.");

        var riga = import.Righe.FirstOrDefault(r => r.Id == request.RigaId)
            ?? throw new KeyNotFoundException($"Riga {request.RigaId} non trovata.");

        riga.ResetRiconciliazione();
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="GeneraMovimentoDaRiga"/>.</summary>
public sealed class GeneraMovimentoDaRigaHandler : IRequestHandler<GeneraMovimentoDaRiga, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GeneraMovimentoDaRigaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GeneraMovimentoDaRigaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(GeneraMovimentoDaRiga request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var import = await db.EstrattiConto
            .Include(e => e.Righe)
            .FirstOrDefaultAsync(e => e.Id == request.ImportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Import {request.ImportId} non trovato.");

        var riga = import.Righe.FirstOrDefault(r => r.Id == request.RigaId)
            ?? throw new KeyNotFoundException($"Riga {request.RigaId} non trovata.");

        var anno = riga.DataContabile.Year;
        var descrizione = string.IsNullOrWhiteSpace(riga.Descrizione)
            ? riga.Operazione ?? "Movimento da estratto conto"
            : riga.Descrizione;

        var movimento = new MovimentoPrimaNota(riga.DataContabile, anno, descrizione, request.CausaleId);
        movimento.UpdateHeader(riga.DataContabile, descrizione, request.CausaleId, null, request.AnagraficaId, null);

        var rigaMov = new RigaMovimento(riga.Importo, request.ContoFinanziarioId, request.CategoriaId);
        rigaMov.SetAliquotaIva(request.AliquotaIvaId);

        if (request.ContoDestinazioneId is { } destId && destId != Guid.Empty)
        {
            var rigaDest = new RigaMovimento(-riga.Importo, destId, request.CategoriaId);
            movimento.ReplaceRighe(new[] { rigaMov, rigaDest });
        }
        else
        {
            movimento.ReplaceRighe(new[] { rigaMov });
        }

        db.Movimenti.Add(movimento);

        riga.Riconcilia(movimento.Id, null);

        await db.SaveChangesAsync(cancellationToken);
        return movimento.Id;
    }
}
