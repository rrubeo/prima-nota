using PrimaNota.Domain.Iva;

namespace PrimaNota.UnitTests.Iva;

public sealed class IvaScorporoTests
{
    [Theory]
    [InlineData(122.00, 22, 100.00, 22.00)]
    [InlineData(110.00, 10, 100.00, 10.00)]
    [InlineData(100.00, 0, 100.00, 0.00)]
    [InlineData(50.00, 5, 47.62, 2.38)]
    public void Scorpora_Standard_Rates(decimal lordo, decimal pct, decimal expectedImponibile, decimal expectedImposta)
    {
        var (imponibile, imposta) = IvaScorporo.Scorpora(lordo, pct);
        imponibile.Should().Be(expectedImponibile);
        imposta.Should().Be(expectedImposta);
    }

    [Fact]
    public void Scorpora_Negative_Amount_Keeps_Sign_On_Base_Rounded_On_Delta()
    {
        var (imponibile, imposta) = IvaScorporo.Scorpora(-122.00m, 22m);
        imponibile.Should().Be(-100.00m);
        imposta.Should().Be(-22.00m);
    }

    [Fact]
    public void Scorpora_Zero_Rate_Leaves_Amount_Untouched()
    {
        var (imponibile, imposta) = IvaScorporo.Scorpora(99.99m, 0m);
        imponibile.Should().Be(99.99m);
        imposta.Should().Be(0m);
    }

    [Fact]
    public void Scorpora_Rounding_Should_Preserve_Sum()
    {
        // 100 + IVA 22% = 122. Starting from 122 we should get back 100 and 22 without drift.
        var (imponibile, imposta) = IvaScorporo.Scorpora(122m, 22m);
        (imponibile + imposta).Should().Be(122m);
    }
}
