using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Azienda;
using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.Iva;

/// <summary>Summary of a periodic VAT liquidation.</summary>
/// <param name="Periodo">The period.</param>
/// <param name="Regime">VAT regime of the fiscal year.</param>
/// <param name="Esigibilita">VAT exigibility applied to the computation.</param>
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
    EsigibilitaIva Esigibilita,
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
    /// <param name="mediator">Mediator (used to compose with RegistroIva under Immediata mode).</param>
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

        var esigibilita = await LoadEsigibilitaAsync(cancellationToken);

        if (esercizio.RegimeIva == RegimeIva.Forfettario)
        {
            return new LiquidazioneIvaDto(
                request.Periodo,
                RegimeIva.Forfettario,
                esigibilita,
                Applicabile: false,
                IvaDebito: 0m,
                IvaCreditoTotale: 0m,
                IvaCreditoIndetraibile: 0m,
                IvaCreditoDetraibile: 0m,
                SaldoPeriodo: 0m,
                CreditoRiportato: 0m,
                SaldoFinale: 0m);
        }

        var aliquoteIndetraibili = await db.AliquoteIva.AsNoTracking()
            .ToDictionaryAsync(a => a.Codice, a => a.PercentualeIndetraibile, cancellationToken);

        var periodo = await ComputePeriodoAsync(request.Periodo, esigibilita, aliquoteIndetraibili, cancellationToken);

        var creditoRiportato = await ComputeCreditoRiportatoAsync(request.Periodo, esigibilita, aliquoteIndetraibili, cancellationToken);

        var saldoFinale = periodo.SaldoPeriodo - creditoRiportato;

        return new LiquidazioneIvaDto(
            request.Periodo,
            RegimeIva.Ordinario,
            esigibilita,
            Applicabile: true,
            IvaDebito: periodo.IvaDebito,
            IvaCreditoTotale: periodo.CreditoTotale,
            IvaCreditoIndetraibile: periodo.CreditoIndetraibile,
            IvaCreditoDetraibile: periodo.CreditoDetraibile,
            SaldoPeriodo: periodo.SaldoPeriodo,
            CreditoRiportato: creditoRiportato,
            SaldoFinale: saldoFinale);
    }

    private async Task<EsigibilitaIva> LoadEsigibilitaAsync(CancellationToken cancellationToken)
    {
        var config = await db.ConfigurazioneAzienda.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ConfigurazioneAzienda.SingletonId, cancellationToken);
        return config?.EsigibilitaIvaPredefinita ?? EsigibilitaIva.Immediata;
    }

    private async Task<PeriodoTotals> ComputePeriodoAsync(
        IvaPeriodo periodo,
        EsigibilitaIva esigibilita,
        IReadOnlyDictionary<string, decimal> aliquoteIndetraibili,
        CancellationToken cancellationToken)
    {
        return esigibilita switch
        {
            EsigibilitaIva.Immediata => await ComputeImmediataAsync(periodo, aliquoteIndetraibili, cancellationToken),
            EsigibilitaIva.Cassa => await ComputeCassaAsync(periodo, aliquoteIndetraibili, cancellationToken),
            _ => throw new InvalidOperationException($"Esigibilità IVA non supportata: {esigibilita}."),
        };
    }

    private async Task<PeriodoTotals> ComputeImmediataAsync(
        IvaPeriodo periodo,
        IReadOnlyDictionary<string, decimal> aliquoteIndetraibili,
        CancellationToken cancellationToken)
    {
        var vendite = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Vendite, periodo), cancellationToken);
        var corrispettivi = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Corrispettivi, periodo), cancellationToken);
        var ivaDebito = vendite.Concat(corrispettivi)
            .Where(r => r.TipoIva == TipoIva.Ordinaria)
            .Sum(r => Math.Abs(r.Imposta));

        var acquisti = await mediator.Send(new GetRegistroIva(TipoRegistroIva.Acquisti, periodo), cancellationToken);

        decimal creditoTotale = 0m;
        decimal creditoIndetraibile = 0m;
        foreach (var row in acquisti.Where(r => r.TipoIva == TipoIva.Ordinaria))
        {
            var imposta = Math.Abs(row.Imposta);
            var pctIndetraibile = aliquoteIndetraibili.GetValueOrDefault(row.AliquotaCodice, 0m);
            var indetraibile = decimal.Round(imposta * pctIndetraibile / 100m, 2, MidpointRounding.ToEven);
            creditoTotale += imposta;
            creditoIndetraibile += indetraibile;
        }

        var creditoDetraibile = creditoTotale - creditoIndetraibile;
        return new PeriodoTotals(ivaDebito, creditoTotale, creditoIndetraibile, creditoDetraibile, ivaDebito - creditoDetraibile);
    }

    private async Task<PeriodoTotals> ComputeCassaAsync(
        IvaPeriodo periodo,
        IReadOnlyDictionary<string, decimal> aliquoteIndetraibili,
        CancellationToken cancellationToken)
    {
        var dataFrom = periodo.DataInizio;
        var dataTo = periodo.DataFine;

        // Load all invoice movements of the year that have at least one payment in the period.
        // Fetch movements + their righe + pagamenti in period; the pure computation is delegated
        // to LiquidazioneProQuotaCalculator for straightforward unit testing.
        // Under IVA per cassa a movement contributes to the period via its pagamenti (when the
        // invoice is settled in instalments) or — for cash sales registered without an explicit
        // Pagamento record — via its own Data when it falls in the period.
        var invoices = await (
            from m in db.Movimenti.AsNoTracking()
            where m.EsercizioAnno == periodo.Anno
                  && (m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled)
                  && (
                      m.Pagamenti.Any(p => p.Data >= dataFrom && p.Data <= dataTo)
                      || (!m.Pagamenti.Any() && m.Data >= dataFrom && m.Data <= dataTo))
            join c in db.Causali.AsNoTracking() on m.CausaleId equals c.Id
            select new
            {
                CausaleTipo = c.Tipo,
                MovimentoData = m.Data,
                HasPagamenti = m.Pagamenti.Any(),
                Righe = m.Righe
                    .Where(r => r.AliquotaIvaId != null)
                    .Select(r => new { r.Importo, r.CategoriaId, r.AliquotaIvaId })
                    .ToList(),
                PagatoInPeriodo = m.Pagamenti
                    .Where(p => p.Data >= dataFrom && p.Data <= dataTo)
                    .Sum(p => p.Importo),
            }).ToListAsync(cancellationToken);

        var categorie = await db.Categorie.AsNoTracking()
            .ToDictionaryAsync(cat => cat.Id, cat => cat.Natura, cancellationToken);
        var aliquote = await db.AliquoteIva.AsNoTracking()
            .ToDictionaryAsync(a => a.Id, a => new { a.Codice, a.Percentuale, a.Tipo }, cancellationToken);

        var fatture = invoices.Select(inv =>
        {
            var totaleLordo = inv.Righe.Sum(r => Math.Abs(r.Importo));

            // For movements without explicit Pagamenti[] (cash sales / corrispettivi) we treat
            // the registration date as the implicit settlement date with ratio 1.
            var pagatoInPeriodo = inv.HasPagamenti ? inv.PagatoInPeriodo : totaleLordo;
            var righe = inv.Righe
                .Where(r => r.AliquotaIvaId is Guid id && aliquote.ContainsKey(id) && categorie.ContainsKey(r.CategoriaId))
                .Select(r =>
                {
                    var al = aliquote[r.AliquotaIvaId!.Value];
                    return new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                        Math.Abs(r.Importo),
                        al.Codice,
                        al.Percentuale,
                        al.Tipo,
                        categorie[r.CategoriaId]);
                })
                .ToList();
            return new LiquidazioneProQuotaCalculator.ProQuotaFattura(
                inv.CausaleTipo,
                totaleLordo,
                pagatoInPeriodo,
                righe);
        });

        var totals = LiquidazioneProQuotaCalculator.Compute(fatture, aliquoteIndetraibili);
        var creditoDetraibile = totals.CreditoTotale - totals.CreditoIndetraibile;
        return new PeriodoTotals(
            totals.IvaDebito,
            totals.CreditoTotale,
            totals.CreditoIndetraibile,
            creditoDetraibile,
            totals.IvaDebito - creditoDetraibile);
    }

    private async Task<decimal> ComputeCreditoRiportatoAsync(
        IvaPeriodo corrente,
        EsigibilitaIva esigibilita,
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
            var totals = await ComputePeriodoAsync(prior, esigibilita, aliquoteIndetraibili, cancellationToken);

            // Only negative saldi (credit) are carried forward; debits are assumed paid.
            cumulatedCredit += totals.SaldoPeriodo < 0 ? -totals.SaldoPeriodo : 0m;
        }

        return cumulatedCredit;
    }

    private readonly record struct PeriodoTotals(
        decimal IvaDebito,
        decimal CreditoTotale,
        decimal CreditoIndetraibile,
        decimal CreditoDetraibile,
        decimal SaldoPeriodo);
}
