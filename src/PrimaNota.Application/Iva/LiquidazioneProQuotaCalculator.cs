using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;

namespace PrimaNota.Application.Iva;

/// <summary>
/// Pure helper that computes the "IVA per cassa" pro-quota contribution to a
/// liquidation period. Given the set of invoices that have at least one payment
/// in the period, it returns the amounts that flow into <c>IVA a debito</c> and
/// <c>IVA a credito</c>. Kept deliberately data-only so it can be unit-tested in
/// isolation from EF Core.
/// </summary>
public static class LiquidazioneProQuotaCalculator
{
    /// <summary>
    /// Computes <see cref="ProQuotaTotals"/> from the set of invoices touched by payments in the period.
    /// The ratio is capped at 1 to defensively handle residual edge cases (credit notes, rounding).
    /// </summary>
    /// <param name="fatture">Invoices having at least one payment in the period.</param>
    /// <param name="percentualeIndetraibilePerCodiceAliquota">Lookup <c>codice → percentuale indetraibile</c>.</param>
    /// <returns>Aggregated period totals.</returns>
    public static ProQuotaTotals Compute(
        IEnumerable<ProQuotaFattura> fatture,
        IReadOnlyDictionary<string, decimal> percentualeIndetraibilePerCodiceAliquota)
    {
        ArgumentNullException.ThrowIfNull(fatture);
        ArgumentNullException.ThrowIfNull(percentualeIndetraibilePerCodiceAliquota);

        decimal ivaDebito = 0m;
        decimal creditoTotale = 0m;
        decimal creditoIndetraibile = 0m;

        foreach (var f in fatture)
        {
            if (f.TotaleLordo <= 0m)
            {
                continue;
            }

            var ratio = Math.Min(f.PagatoInPeriodo / f.TotaleLordo, 1m);
            if (ratio <= 0m)
            {
                continue;
            }

            foreach (var riga in f.Righe)
            {
                if (riga.AliquotaTipo != TipoIva.Ordinaria)
                {
                    continue;
                }

                var (_, imposta) = IvaScorporo.Scorpora(riga.ImportoLordo, riga.AliquotaPercentuale);
                var impostaProQuota = decimal.Round(imposta * ratio, 2, MidpointRounding.ToEven);
                if (impostaProQuota == 0m)
                {
                    continue;
                }

                switch (f.CausaleTipo)
                {
                    case TipoMovimento.Incasso when riga.Natura == NaturaCategoria.Entrata:
                        ivaDebito += impostaProQuota;
                        break;
                    case TipoMovimento.Pagamento when riga.Natura == NaturaCategoria.Uscita:
                    case TipoMovimento.RimborsoNotaSpese when riga.Natura == NaturaCategoria.Uscita:
                        var pctIndetraibile = percentualeIndetraibilePerCodiceAliquota.GetValueOrDefault(riga.AliquotaCodice, 0m);
                        var indetraibile = decimal.Round(impostaProQuota * pctIndetraibile / 100m, 2, MidpointRounding.ToEven);
                        creditoTotale += impostaProQuota;
                        creditoIndetraibile += indetraibile;
                        break;
                    default:
                        break;
                }
            }
        }

        return new ProQuotaTotals(ivaDebito, creditoTotale, creditoIndetraibile);
    }

    /// <summary>Single line of a VAT-bearing invoice.</summary>
    /// <param name="ImportoLordo">Absolute line amount (always &gt;= 0).</param>
    /// <param name="AliquotaCodice">VAT rate code used to look up the indetraibile percentage.</param>
    /// <param name="AliquotaPercentuale">VAT rate percentage (e.g. 22m).</param>
    /// <param name="AliquotaTipo">Rate kind — only <see cref="TipoIva.Ordinaria"/> contributes.</param>
    /// <param name="Natura">Category nature (Entrata = sales side, Uscita = purchase side).</param>
    public readonly record struct ProQuotaRiga(
        decimal ImportoLordo,
        string AliquotaCodice,
        decimal AliquotaPercentuale,
        TipoIva AliquotaTipo,
        NaturaCategoria Natura);

    /// <summary>Invoice projection used by the calculator.</summary>
    /// <param name="CausaleTipo">Kind of the invoice causale (Incasso / Pagamento / …).</param>
    /// <param name="TotaleLordo">Absolute invoice total used as the denominator of the pro-quota ratio.</param>
    /// <param name="PagatoInPeriodo">Sum of payments whose Data falls in the period.</param>
    /// <param name="Righe">VAT-bearing lines of the invoice.</param>
    public readonly record struct ProQuotaFattura(
        TipoMovimento CausaleTipo,
        decimal TotaleLordo,
        decimal PagatoInPeriodo,
        IReadOnlyList<ProQuotaRiga> Righe);

    /// <summary>Aggregated totals returned to the liquidation handler.</summary>
    /// <param name="IvaDebito">Pro-quota VAT flowing into "IVA a debito".</param>
    /// <param name="CreditoTotale">Pro-quota VAT flowing into input VAT (before indetraibile).</param>
    /// <param name="CreditoIndetraibile">Portion of CreditoTotale marked as non-deductible.</param>
    public readonly record struct ProQuotaTotals(
        decimal IvaDebito,
        decimal CreditoTotale,
        decimal CreditoIndetraibile);
}
