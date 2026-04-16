using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.ContiFinanziari;

/// <summary>
/// Computes the current balance of a single financial account as
/// <c>SaldoIniziale + sum(righe.Importo)</c> across all confirmed / reconciled
/// movements after <c>DataSaldoIniziale</c>. Draft movements are excluded.
/// </summary>
/// <param name="ContoFinanziarioId">Account id.</param>
public sealed record GetSaldoConto(Guid ContoFinanziarioId) : IRequest<decimal>;

/// <summary>Handler for <see cref="GetSaldoConto"/>.</summary>
public sealed class GetSaldoContoHandler : IRequestHandler<GetSaldoConto, decimal>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetSaldoContoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetSaldoContoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<decimal> Handle(GetSaldoConto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conto = await db.ContiFinanziari
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContoFinanziarioId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conto {request.ContoFinanziarioId} non trovato.");

        var movementsSum = await db.Movimenti
            .AsNoTracking()
            .Where(m =>
                m.Data >= conto.DataSaldoIniziale &&
                (m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled))
            .SelectMany(m => m.Righe)
            .Where(r => r.ContoFinanziarioId == request.ContoFinanziarioId)
            .SumAsync(r => (decimal?)r.Importo, cancellationToken) ?? 0m;

        return conto.SaldoIniziale + movementsSum;
    }
}
