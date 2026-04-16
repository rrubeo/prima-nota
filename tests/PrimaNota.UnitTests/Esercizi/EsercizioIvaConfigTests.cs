using PrimaNota.Domain.Esercizi;
using PrimaNota.Domain.Iva;

namespace PrimaNota.UnitTests.Esercizi;

public sealed class EsercizioIvaConfigTests
{
    [Fact]
    public void Default_Regime_Should_Be_Ordinario_Trimestrale()
    {
        var e = new EsercizioContabile(2026);

        e.RegimeIva.Should().Be(RegimeIva.Ordinario);
        e.PeriodicitaIva.Should().Be(PeriodicitaIva.Trimestrale);
        e.CoefficienteRedditivitaForfettario.Should().BeNull();
    }

    [Fact]
    public void Configure_Ordinario_Should_Clear_Coefficient()
    {
        var e = new EsercizioContabile(2026);
        e.ConfiguraIva(RegimeIva.Forfettario, PeriodicitaIva.Trimestrale, 78m);
        e.CoefficienteRedditivitaForfettario.Should().Be(78m);

        e.ConfiguraIva(RegimeIva.Ordinario, PeriodicitaIva.Mensile, null);

        e.RegimeIva.Should().Be(RegimeIva.Ordinario);
        e.PeriodicitaIva.Should().Be(PeriodicitaIva.Mensile);
        e.CoefficienteRedditivitaForfettario.Should().BeNull();
    }

    [Fact]
    public void Configure_Forfettario_Without_Coefficient_Should_Throw()
    {
        var e = new EsercizioContabile(2026);
        var act = () => e.ConfiguraIva(RegimeIva.Forfettario, PeriodicitaIva.Trimestrale, null);
        act.Should().Throw<ArgumentException>().WithMessage("*coefficiente*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(150)]
    public void Configure_Forfettario_With_Invalid_Coefficient_Should_Throw(decimal coeff)
    {
        var e = new EsercizioContabile(2026);
        var act = () => e.ConfiguraIva(RegimeIva.Forfettario, PeriodicitaIva.Trimestrale, coeff);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Configure_On_Closed_Exercise_Should_Throw()
    {
        var e = new EsercizioContabile(2026);
        e.Chiudi(DateTimeOffset.UtcNow);

        var act = () => e.ConfiguraIva(RegimeIva.Ordinario, PeriodicitaIva.Mensile, null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*chius*");
    }
}
