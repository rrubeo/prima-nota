using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.UnitTests.ContiFinanziari;

public sealed class RegolaSignatureTests
{
    [Fact]
    public void NormalizeCode_TrimsAndUppercases()
    {
        RegolaSignature.NormalizeCode("  bonifico sepa ").Should().Be("BONIFICO SEPA");
        RegolaSignature.NormalizeCode("48").Should().Be("48");
    }

    [Fact]
    public void NormalizeCode_EmptyOrNull_ReturnsEmpty()
    {
        RegolaSignature.NormalizeCode(null).Should().BeEmpty();
        RegolaSignature.NormalizeCode("   ").Should().BeEmpty();
    }

    [Fact]
    public void DescrizioneChiave_KeepsCounterpartyAndDropsVolatileTokens()
    {
        // Codes (CID/MAN), IBAN-like and alphanumeric blobs are dropped; the leasing company stays.
        RegolaSignature.ComputeDescrizioneChiave(
            "ARVAL SERVICE LEASE  CID.IT970010000000879960524 MAN.C063040000000000N47372")
            .Should().Be("arval service lease");
    }

    [Fact]
    public void DescrizioneChiave_DropsDatesTimesCardsAndOpCodes()
    {
        RegolaSignature.ComputeDescrizioneChiave(
            "AMAZON* ZG0NN7E54      30/12/2025 16.45 LUXEMBOURG    Op.692982 carta ****4159")
            .Should().Be("amazon luxembourg");
    }

    [Fact]
    public void DescrizioneChiave_DistintaOnly_IsEmpty()
    {
        RegolaSignature.ComputeDescrizioneChiave("Distinta: 176911272442716O8DEF").Should().BeEmpty();
    }

    [Fact]
    public void DescrizioneChiave_RecurringFee_CollapsesToEmptyRegardlessOfMonth()
    {
        // "tenuta conto" style descriptions differ only by month -> they must collapse to the
        // same (empty) key so the rule matches on cause + operation alone.
        var dicembre = RegolaSignature.ComputeDescrizioneChiave("ADDEBITO RELATIVO AL PERIODO DI DICEMBRE  2025");
        var gennaio = RegolaSignature.ComputeDescrizioneChiave("ADDEBITO RELATIVO AL PERIODO DI GENNAIO 2026");

        dicembre.Should().BeEmpty();
        gennaio.Should().BeEmpty();
    }

    [Fact]
    public void DescrizioneChiave_CapsAtThreeTokens()
    {
        RegolaSignature.ComputeDescrizioneChiave(
            "Da SIRIO SERVICE 2004 SOCIETA' COOPERATIVA per SALDO FT. 2/26 DEL 20/01/2026")
            .Should().Be("sirio service societa");
    }

    [Fact]
    public void DescrizioneChiave_NullOrEmpty_ReturnsEmpty()
    {
        RegolaSignature.ComputeDescrizioneChiave(null).Should().BeEmpty();
        RegolaSignature.ComputeDescrizioneChiave("   ").Should().BeEmpty();
    }

    [Fact]
    public void Compute_ProducesFullKey()
    {
        var key = RegolaSignature.Compute("48", "BONIFICO SEPA", "Da SIRIO SERVICE 2004 per SALDO");

        key.CausaleOperazione.Should().Be("48");
        key.Operazione.Should().Be("BONIFICO SEPA");
        key.DescrizioneChiave.Should().Be("sirio service saldo");
    }

    [Fact]
    public void Compute_SameCounterpartyDifferentAmountsAndDates_YieldsSameKey()
    {
        var a = RegolaSignature.Compute("151F", "RATA FINANZIAMENTO", "ARVAL SERVICE LEASE CID.IT1 MAN.A 326,90 02/01/2026");
        var b = RegolaSignature.Compute("151F", "RATA FINANZIAMENTO", "ARVAL SERVICE LEASE CID.IT2 MAN.B 410,15 02/02/2026");

        a.Should().Be(b);
    }
}
