using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PianoConti;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.Anagrafiche;

/// <summary>Kind of row in the account statement.</summary>
public enum SchedaRigaTipo
{
    /// <summary>Invoice (or invoice-like movement) that increases the receivable / payable.</summary>
    Fattura = 1,

    /// <summary>Payment (or collection) that reduces the receivable / payable.</summary>
    Pagamento = 2,
}

/// <summary>A single row of the account statement (scheda cliente/fornitore).</summary>
/// <param name="Data">Value date shown on the ledger.</param>
/// <param name="Tipo">Kind of row.</param>
/// <param name="Descrizione">Display description.</param>
/// <param name="Numero">Invoice number (when applicable).</param>
/// <param name="Dare">Amount in "dare" column (customers: invoice issued; suppliers: payment made).</param>
/// <param name="Avere">Amount in "avere" column (customers: payment received; suppliers: invoice received).</param>
/// <param name="Saldo">Running balance after this row. Positive = residual receivable (client owes); negative = residual payable (we owe supplier).</param>
/// <param name="MovimentoId">Parent movement id for navigation.</param>
/// <param name="PagamentoId">Payment id when the row is a payment.</param>
public sealed record SchedaAnagraficaRigaDto(
    DateOnly Data,
    SchedaRigaTipo Tipo,
    string Descrizione,
    string? Numero,
    decimal Dare,
    decimal Avere,
    decimal Saldo,
    Guid MovimentoId,
    Guid? PagamentoId);

/// <summary>Result of the scheda query.</summary>
/// <param name="AnagraficaId">Identifier of the anagrafica.</param>
/// <param name="RagioneSociale">Display name.</param>
/// <param name="Dare">Total "dare" across all rows.</param>
/// <param name="Avere">Total "avere" across all rows.</param>
/// <param name="SaldoFinale">Final balance (Dare − Avere). Positive = net receivable, negative = net payable.</param>
/// <param name="Righe">Ordered rows with running balance.</param>
public sealed record SchedaAnagraficaDto(
    Guid AnagraficaId,
    string RagioneSociale,
    decimal Dare,
    decimal Avere,
    decimal SaldoFinale,
    IReadOnlyList<SchedaAnagraficaRigaDto> Righe);

/// <summary>Builds the account statement (scheda cliente/fornitore) for an anagrafica in a period.</summary>
/// <param name="AnagraficaId">Anagrafica id.</param>
/// <param name="Anno">Fiscal year used to bound the selection (null = all years in the DB).</param>
/// <param name="DataFrom">Inclusive lower bound.</param>
/// <param name="DataTo">Inclusive upper bound.</param>
public sealed record GetSchedaAnagrafica(
    Guid AnagraficaId,
    int? Anno = null,
    DateOnly? DataFrom = null,
    DateOnly? DataTo = null) : IRequest<SchedaAnagraficaDto?>;

/// <summary>Handler for <see cref="GetSchedaAnagrafica"/>.</summary>
public sealed class GetSchedaAnagraficaHandler : IRequestHandler<GetSchedaAnagrafica, SchedaAnagraficaDto?>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetSchedaAnagraficaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetSchedaAnagraficaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<SchedaAnagraficaDto?> Handle(GetSchedaAnagrafica request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var anagrafica = await db.Anagrafiche.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AnagraficaId, cancellationToken);
        if (anagrafica is null)
        {
            return null;
        }

        // Load all movements having this counterparty (on header or on at least one line).
        var query = db.Movimenti.AsNoTracking()
            .Include(m => m.Righe)
            .Include(m => m.Pagamenti)
            .Where(m => m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled)
            .Where(m => m.AnagraficaId == request.AnagraficaId
                        || m.Righe.Any(r => r.AnagraficaId == request.AnagraficaId));

        if (request.Anno is { } anno)
        {
            query = query.Where(m => m.EsercizioAnno == anno);
        }

        if (request.DataFrom is { } from)
        {
            query = query.Where(m => m.Data >= from);
        }

        if (request.DataTo is { } to)
        {
            query = query.Where(m => m.Data <= to);
        }

        var movimenti = await query.ToListAsync(cancellationToken);
        if (movimenti.Count == 0)
        {
            return new SchedaAnagraficaDto(anagrafica.Id, anagrafica.RagioneSociale, 0m, 0m, 0m, Array.Empty<SchedaAnagraficaRigaDto>());
        }

        // Load causale kind for each movement to decide dare/avere assignment.
        var causaleIds = movimenti.Select(m => m.CausaleId).Distinct().ToList();
        var causaliById = await db.Causali.AsNoTracking()
            .Where(c => causaleIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new { c.Codice, c.Nome, c.Tipo }, cancellationToken);

        var righe = new List<SchedaAnagraficaRigaDto>(movimenti.Count * 2);
        foreach (var m in movimenti)
        {
            if (!causaliById.TryGetValue(m.CausaleId, out var causale))
            {
                continue;
            }

            var (dareFattura, avereFattura) = DareAvereFattura(causale.Tipo, Math.Abs(m.Totale));

            righe.Add(new SchedaAnagraficaRigaDto(
                m.Data,
                SchedaRigaTipo.Fattura,
                string.IsNullOrWhiteSpace(m.Descrizione) ? causale.Nome : m.Descrizione,
                m.Numero,
                dareFattura,
                avereFattura,
                Saldo: 0m,
                MovimentoId: m.Id,
                PagamentoId: null));

            foreach (var p in m.Pagamenti)
            {
                var (darePag, averePag) = DareAverePagamento(causale.Tipo, p.Importo);
                righe.Add(new SchedaAnagraficaRigaDto(
                    p.Data,
                    SchedaRigaTipo.Pagamento,
                    $"Pagamento {causale.Nome}".TrimEnd(),
                    m.Numero,
                    darePag,
                    averePag,
                    Saldo: 0m,
                    MovimentoId: m.Id,
                    PagamentoId: p.Id));
            }
        }

        // Chronological order: fattura precedes its own payments when on the same day.
        var ordered = righe
            .OrderBy(r => r.Data)
            .ThenBy(r => r.Tipo == SchedaRigaTipo.Fattura ? 0 : 1)
            .ToList();

        decimal running = 0m;
        var withSaldo = new List<SchedaAnagraficaRigaDto>(ordered.Count);
        foreach (var r in ordered)
        {
            running += r.Dare - r.Avere;
            withSaldo.Add(r with { Saldo = running });
        }

        var totDare = ordered.Sum(r => r.Dare);
        var totAvere = ordered.Sum(r => r.Avere);

        return new SchedaAnagraficaDto(
            anagrafica.Id,
            anagrafica.RagioneSociale,
            totDare,
            totAvere,
            totDare - totAvere,
            withSaldo);
    }

    /// <summary>
    /// Maps (causale kind, invoice amount) to (dare, avere) on the ledger row of the invoice itself.
    /// Customer invoices (Incasso): Dare = importo (client owes). Supplier invoices (Pagamento / RimborsoNotaSpese):
    /// Avere = importo (we owe). Other kinds do not contribute to the ledger.
    /// </summary>
    private static (decimal Dare, decimal Avere) DareAvereFattura(TipoMovimento tipo, decimal importo) => tipo switch
    {
        TipoMovimento.Incasso => (importo, 0m),
        TipoMovimento.Pagamento => (0m, importo),
        TipoMovimento.RimborsoNotaSpese => (0m, importo),
        _ => (0m, 0m),
    };

    /// <summary>Payments reverse the invoice row: a client payment goes into avere; a supplier payment into dare.</summary>
    private static (decimal Dare, decimal Avere) DareAverePagamento(TipoMovimento tipo, decimal importo) => tipo switch
    {
        TipoMovimento.Incasso => (0m, importo),
        TipoMovimento.Pagamento => (importo, 0m),
        TipoMovimento.RimborsoNotaSpese => (importo, 0m),
        _ => (0m, 0m),
    };
}
