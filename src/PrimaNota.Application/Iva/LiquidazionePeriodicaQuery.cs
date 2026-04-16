using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Application.Iva;

/// <summary>Summary of a periodic VAT liquidation.</summary>
/// <param name="Periodo">The period.</param>
/// <param name="Regime">VAT regime of the fiscal year.</param>
/// <param name="Applicabile">False when regime is Forfettario (no liquidation).</param>
/// <param name="IvaDebito">Output VAT (vendite + corrispettivi) for the period.</param>
/// <param name="IvaCreditoTotale">Input VAT (acquisti) for the period, before non-deductible adjustment.</param>
/// <param name="IvaCreditoIndetraibile">Portion of input VAT marked non-deductible by the rate.</param>
/// <param name="IvaCreditoDetraibile">Input VAT actually deductible (= totale − indetraibile).</param>
/// <param name="SaldoPeriodo">Debito − credito detraibile. Positive = debito, negative = credito.</param>
/// <param name="CreditoRiportato">Credit carried forward from prior periods of the same year.</param>
/// <param name="SaldoFinale">SaldoPeriodo − creditoRiportato, positive means amount due.</param>
public sealed record LiquidazioneIvaDto(
    IvaPeriodo Periodo,
    RegimeIva Regime,
    bool Applicabile,
    decimal IvaDebito,
    decimal IvaCreditoTotale,
    decimal IvaCreditoIndetraibile,
    decimal IvaCreditoDetraibile,
    decimal SaldoPeriodo,
    decimal CreditoRiportato,
    decimal SaldoFinale);

/// <summary>Computes the VAT liquidation for the given period.</summary>
/// <param name="Periodo">Period.</param>
public sealed record GetLiquidazioneIva(IvaPeriodo Periodo) : IRequest<LiquidazioneIvaDto>;

/// <summary>Handler for <see cref="GetLiquidazioneIva"/>.</summary>
public sealed class GetLiquidazioneIvaHandler : IRequestHandler<GetLiquidazioneIva, LiquidazioneIvaDto>
{
    private readonly IMediator mediator;
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetLiquidazioneIvaHandler"/> class.</summary>
    /// <param name="mediator">Mediator (used to compose with RegistroIva).</param>
    /// <param name="db">DB.</param>
    public GetLiquidazioneIvaHandler(IMediator mediator, IApplicationDbContext db)
    {
        this.mediator = mediator;
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<LiquidazioneIvaDto> Handle(GetLiquidazioneIva request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var esercizio = await db.Esercizi
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Anno == request.Periodo.Anno, cancellationToken);

        if (esercizio is null)
        {
            throw new KeyNotFoundException($"Esercizio {request.Periodo.Anno} non trovato.");
        }

        if (esercizio.RegimeIva == RegimeIva.Forfettario)
        {
            return new LiquidazioneIvaDto(
                request.Periodo,
                RegimeIva.Forfettario,
                Applicabile: false,
                IvaDebito: 0m,
                IvaCreditoTotale: 0m,
                IvaCreditoIndetraibile: 0m,
                IvaCreditoDetraibile: 0m,
                SaldoPeriodo: 0m,
                CreditoRiportato: 0m,
                SaldoFinale: 0m);
        }

        // Vendite + corrispettivi = debito (imposta positiva su movimenti di entrata)
        var vendite = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Vendite, request.Periodo), cancellationToken);
        var corrispettivi = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Corrispettivi, request.Periodo), cancellationToken);
        var ivaDebito = vendite.Concat(corrispettivi)
            .Where(r => r.TipoIva == TipoIva.Ordinaria)
            .Sum(r => Math.Abs(r.Imposta));

        // Acquisti = credito (imposta positiva dal valore assoluto)
        var acquisti = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Acquisti, request.Periodo), cancellationToken);

        // Enrich with the non-deductible percentage read from the rate.
        var aliquoteById = await db.AliquoteIva.AsNoTracking()
            .ToDictionaryAsync(a => a.Codice, a => a.PercentualeIndetraibile, cancellationToken);

        decimal creditoTotale = 0m;
        decimal creditoIndetraibile = 0m;
        foreach (var row in acquisti.Where(r => r.TipoIva == TipoIva.Ordinaria))
        {
            var imposta = Math.Abs(row.Imposta);
            var pctIndetraibile = aliquoteById.GetValueOrDefault(row.AliquotaCodice, 0m);
            var indetraibile = decimal.Round(imposta * pctIndetraibile / 100m, 2, MidpointRounding.ToEven);
            creditoTotale += imposta;
            creditoIndetraibile += indetraibile;
        }

        var creditoDetraibile = creditoTotale - creditoIndetraibile;
        var saldoPeriodo = ivaDebito - creditoDetraibile;

        // Credit carried over from prior periods of the same year.
        var creditoRiportato = await ComputeCreditoRiportatoAsync(request.Periodo, aliquoteById, cancellationToken);

        var saldoFinale = saldoPeriodo - creditoRiportato;

        return new LiquidazioneIvaDto(
            request.Periodo,
            RegimeIva.Ordinario,
            Applicabile: true,
            IvaDebito: ivaDebito,
            IvaCreditoTotale: creditoTotale,
            IvaCreditoIndetraibile: creditoIndetraibile,
            IvaCreditoDetraibile: creditoDetraibile,
            SaldoPeriodo: saldoPeriodo,
            CreditoRiportato: creditoRiportato,
            SaldoFinale: saldoFinale);
    }

    private async Task<decimal> ComputeCreditoRiportatoAsync(
        IvaPeriodo corrente,
        IReadOnlyDictionary<string, decimal> aliquoteIndetraibili,
        CancellationToken cancellationToken)
    {
        if (corrente.Indice <= 1)
        {
            return 0m;
        }

        decimal cumulatedCredit = 0m;
        for (var i = 1; i < corrente.Indice; i++)
        {
            var prior = new IvaPeriodo(corrente.Anno, corrente.Periodicita, i);
            var vendite = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Vendite, prior), cancellationToken);
            var corrispettivi = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Corrispettivi, prior), cancellationToken);
            var acquisti = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Acquisti, prior), cancellationToken);

            var debito = vendite.Concat(corrispettivi).Where(r => r.TipoIva == TipoIva.Ordinaria).Sum(r => Math.Abs(r.Imposta));

            decimal credito = 0m;
            foreach (var row in acquisti.Where(r => r.TipoIva == TipoIva.Ordinaria))
            {
                var imposta = Math.Abs(row.Imposta);
                var indetraibile = decimal.Round(
                    imposta * aliquoteIndetraibili.GetValueOrDefault(row.AliquotaCodice, 0m) / 100m,
                    2,
                    MidpointRounding.ToEven);
                credito += imposta - indetraibile;
            }

            var saldoPrior = debito - credito;

            // Only negative saldi (credit) are carried forward; debits are assumed paid.
            cumulatedCredit += saldoPrior < 0 ? -saldoPrior : 0m;
        }

        return cumulatedCredit;
    }
}
