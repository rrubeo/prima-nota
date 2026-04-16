using PrimaNota.Application.Iva;
using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;

namespace PrimaNota.UnitTests.Iva;

public sealed class LiquidazioneProQuotaCalculatorTests
{
    private static readonly IReadOnlyDictionary<string, decimal> NoIndetraibile = new Dictionary<string, decimal>();

    [Fact]
    public void No_Invoices_Should_Return_Zero_Totals()
    {
        var totals = LiquidazioneProQuotaCalculator.Compute(
            Array.Empty<LiquidazioneProQuotaCalculator.ProQuotaFattura>(),
            NoIndetraibile);

        totals.IvaDebito.Should().Be(0m);
        totals.CreditoTotale.Should().Be(0m);
        totals.CreditoIndetraibile.Should().Be(0m);
    }

    [Fact]
    public void Fattura_Attiva_Incasso_Integrale_Should_Compute_Full_Vat()
    {
        // Fattura cliente: imponibile 1000 + IVA 22% = 1220 lordo, incasso 1220.
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Incasso,
            TotaleLordo: 1220m,
            PagatoInPeriodo: 1220m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    1220m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(220m);
        totals.CreditoTotale.Should().Be(0m);
    }

    [Fact]
    public void Fattura_Attiva_Acconto_Should_Produce_Pro_Quota_Vat()
    {
        // Fattura cliente 1220, incasso nel periodo = 610 → ratio 0,5 → IVA pro-quota = 110.
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Incasso,
            TotaleLordo: 1220m,
            PagatoInPeriodo: 610m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    1220m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(110m);
    }

    [Fact]
    public void Fattura_Passiva_Acquisto_Should_Flow_To_Credito()
    {
        // Fattura fornitore 488 (imponibile 400 + IVA 22% = 488), pagamento integrale.
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Pagamento,
            TotaleLordo: 488m,
            PagatoInPeriodo: 488m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    488m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Uscita),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(0m);
        totals.CreditoTotale.Should().Be(88m);
        totals.CreditoIndetraibile.Should().Be(0m);
    }

    [Fact]
    public void Indetraibile_Percentage_Should_Reduce_Credito()
    {
        // Auto aziendale al 40% deducibile (60% indetraibile).
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Pagamento,
            TotaleLordo: 1220m,
            PagatoInPeriodo: 1220m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    1220m, "IVA22-AUTO", 22m, TipoIva.Ordinaria, NaturaCategoria.Uscita),
            });
        var indetraibili = new Dictionary<string, decimal> { ["IVA22-AUTO"] = 60m };

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, indetraibili);

        totals.CreditoTotale.Should().Be(220m);
        totals.CreditoIndetraibile.Should().Be(132m);
    }

    [Fact]
    public void Multiple_Rate_Rows_Should_Be_Summed_Independently()
    {
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Incasso,
            TotaleLordo: 1420m,
            PagatoInPeriodo: 710m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    1220m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    200m, "IVA-ESENTE", 0m, TipoIva.Esente, NaturaCategoria.Entrata),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        // Only 22% row contributes; ratio 0,5 on imposta 220 → 110.
        totals.IvaDebito.Should().Be(110m);
    }

    [Fact]
    public void OverPayment_Should_Be_Capped_At_Full_Invoice()
    {
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Incasso,
            TotaleLordo: 1220m,
            PagatoInPeriodo: 2000m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    1220m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(220m);
    }

    [Fact]
    public void Zero_Total_Invoice_Should_Be_Ignored()
    {
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Incasso,
            TotaleLordo: 0m,
            PagatoInPeriodo: 100m,
            Righe: Array.Empty<LiquidazioneProQuotaCalculator.ProQuotaRiga>());

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(0m);
    }

    [Fact]
    public void No_Payment_In_Period_Should_Contribute_Zero()
    {
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Incasso,
            TotaleLordo: 1220m,
            PagatoInPeriodo: 0m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    1220m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(0m);
    }

    [Fact]
    public void Giroconto_Causale_Should_Not_Contribute()
    {
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.GirocontoInterno,
            TotaleLordo: 500m,
            PagatoInPeriodo: 500m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    500m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(0m);
        totals.CreditoTotale.Should().Be(0m);
    }

    [Fact]
    public void Multiple_Invoices_Should_Accumulate()
    {
        var fatture = new[]
        {
            new LiquidazioneProQuotaCalculator.ProQuotaFattura(
                TipoMovimento.Incasso, 1220m, 1220m,
                new[]
                {
                    new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                        1220m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
                }),
            new LiquidazioneProQuotaCalculator.ProQuotaFattura(
                TipoMovimento.Pagamento, 488m, 244m,
                new[]
                {
                    new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                        488m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Uscita),
                }),
        };

        var totals = LiquidazioneProQuotaCalculator.Compute(fatture, NoIndetraibile);

        totals.IvaDebito.Should().Be(220m);
        totals.CreditoTotale.Should().Be(44m);
    }

    [Fact]
    public void Rounding_Should_Apply_Banker_Convention()
    {
        // Lordo 101 al 22% → imponibile 82,79 IVA 18,21. Pagato 50,50 → ratio ~0,5 → IVA pro-quota 9,105 → ToEven = 9,10.
        var fattura = new LiquidazioneProQuotaCalculator.ProQuotaFattura(
            TipoMovimento.Incasso,
            TotaleLordo: 101m,
            PagatoInPeriodo: 50.50m,
            Righe: new[]
            {
                new LiquidazioneProQuotaCalculator.ProQuotaRiga(
                    101m, "IVA22", 22m, TipoIva.Ordinaria, NaturaCategoria.Entrata),
            });

        var totals = LiquidazioneProQuotaCalculator.Compute(new[] { fattura }, NoIndetraibile);

        totals.IvaDebito.Should().Be(9.10m);
    }
}
