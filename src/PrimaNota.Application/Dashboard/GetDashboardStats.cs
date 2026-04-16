using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PianoConti;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.Dashboard;

/// <summary>KPI snapshot shown on the home dashboard.</summary>
/// <param name="SaldoTotale">Sum of the balances of all active financial accounts.</param>
/// <param name="SaldoCassa">Balance of the "Cassa" accounts only.</param>
/// <param name="SaldoBanche">Balance of the "Banca" accounts only.</param>
/// <param name="SaldoCarte">Balance of card accounts.</param>
/// <param name="EntrateMeseCorrente">Positive line sum for the current month.</param>
/// <param name="UsciteMeseCorrente">Absolute value of the negative line sum for the current month.</param>
/// <param name="MovimentiDraftCount">Movements currently in Draft state.</param>
/// <param name="MovimentiYtdCount">Confirmed or reconciled movements of the year.</param>
public sealed record DashboardStats(
    decimal SaldoTotale,
    decimal SaldoCassa,
    decimal SaldoBanche,
    decimal SaldoCarte,
    decimal EntrateMeseCorrente,
    decimal UsciteMeseCorrente,
    int MovimentiDraftCount,
    int MovimentiYtdCount);

/// <summary>Retrieves the dashboard statistics for a given year.</summary>
/// <param name="Anno">Fiscal year (current if null).</param>
public sealed record GetDashboardStats(int Anno) : IRequest<DashboardStats>;

/// <summary>Handler for <see cref="GetDashboardStats"/>.</summary>
public sealed class GetDashboardStatsHandler : IRequestHandler<GetDashboardStats, DashboardStats>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetDashboardStatsHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetDashboardStatsHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<DashboardStats> Handle(GetDashboardStats request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        var activeAccounts = await db.ContiFinanziari
            .AsNoTracking()
            .Where(c => c.Attivo)
            .Select(c => new { c.Id, c.Tipo, c.SaldoIniziale, c.DataSaldoIniziale })
            .ToListAsync(cancellationToken);

        var confirmedMovements = db.Movimenti
            .AsNoTracking()
            .Where(m => m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled);

        var lineSums = await confirmedMovements
            .SelectMany(m => m.Righe.Select(r => new { r.ContoFinanziarioId, r.Importo, m.Data }))
            .ToListAsync(cancellationToken);

        decimal SaldoConto(Guid contoId, DateOnly since) =>
            lineSums.Where(x => x.ContoFinanziarioId == contoId && x.Data >= since).Sum(x => x.Importo);

        decimal saldoTotale = 0m, saldoCassa = 0m, saldoBanche = 0m, saldoCarte = 0m;
        foreach (var c in activeAccounts)
        {
            var saldo = c.SaldoIniziale + SaldoConto(c.Id, c.DataSaldoIniziale);
            saldoTotale += saldo;
            switch (c.Tipo)
            {
                case Domain.ContiFinanziari.TipoConto.Cassa: saldoCassa += saldo; break;
                case Domain.ContiFinanziari.TipoConto.Banca: saldoBanche += saldo; break;
                case Domain.ContiFinanziari.TipoConto.CartaDiCredito:
                case Domain.ContiFinanziari.TipoConto.CartaDebitoPrepagata: saldoCarte += saldo; break;
            }
        }

        var mese = await confirmedMovements
            .Where(m => m.Data >= firstOfMonth)
            .SelectMany(m => m.Righe)
            .Join(
                db.Categorie.AsNoTracking(),
                r => r.CategoriaId,
                c => c.Id,
                (r, c) => new { r.Importo, c.Natura })
            .ToListAsync(cancellationToken);

        var entrate = mese.Where(x => x.Natura == NaturaCategoria.Entrata).Sum(x => x.Importo);
        var uscite = Math.Abs(mese.Where(x => x.Natura == NaturaCategoria.Uscita).Sum(x => x.Importo));

        var draftCount = await db.Movimenti
            .AsNoTracking()
            .CountAsync(m => m.Stato == StatoMovimento.Draft && m.EsercizioAnno == request.Anno, cancellationToken);

        var ytdCount = await db.Movimenti
            .AsNoTracking()
            .CountAsync(
                m => m.EsercizioAnno == request.Anno &&
                     (m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled),
                cancellationToken);

        return new DashboardStats(
            saldoTotale,
            saldoCassa,
            saldoBanche,
            saldoCarte,
            entrate,
            uscite,
            draftCount,
            ytdCount);
    }
}
