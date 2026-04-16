using System.Text;
using PrimaNota.Application.PrimaNota.Import;

namespace PrimaNota.UnitTests.PrimaNota;

public sealed class FatturaElettronicaParserTests
{
    private const string SampleXml = """
<?xml version="1.0" encoding="utf-8"?>
<FatturaElettronica versione="FPR12" xmlns="http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2">
  <FatturaElettronicaHeader xmlns="">
    <DatiTrasmissione>
      <IdTrasmittente><IdPaese>IT</IdPaese><IdCodice>01879020517</IdCodice></IdTrasmittente>
      <ProgressivoInvio>1</ProgressivoInvio>
      <FormatoTrasmissione>FPR12</FormatoTrasmissione>
      <CodiceDestinatario>XXXXXXX</CodiceDestinatario>
    </DatiTrasmissione>
    <CedentePrestatore>
      <DatiAnagrafici>
        <IdFiscaleIVA><IdPaese>IT</IdPaese><IdCodice>14450591004</IdCodice></IdFiscaleIVA>
        <CodiceFiscale>14450591004</CodiceFiscale>
        <Anagrafica><Denominazione>Jadis Consulting SRL</Denominazione></Anagrafica>
        <RegimeFiscale>RF01</RegimeFiscale>
      </DatiAnagrafici>
      <Sede>
        <Indirizzo>Via Proba Petronia, 96</Indirizzo>
        <CAP>00136</CAP>
        <Comune>Roma</Comune>
        <Provincia>RM</Provincia>
        <Nazione>IT</Nazione>
      </Sede>
      <Contatti><Email>amministrazione@jadisconsulting.com</Email></Contatti>
    </CedentePrestatore>
    <CessionarioCommittente>
      <DatiAnagrafici>
        <IdFiscaleIVA><IdPaese>MT</IdPaese><IdCodice>MT31185926</IdCodice></IdFiscaleIVA>
        <Anagrafica><Denominazione>Omniversity Edutech LTD</Denominazione></Anagrafica>
      </DatiAnagrafici>
      <Sede>
        <Indirizzo>Giuseppe Cali Street, XBX1425</Indirizzo>
        <CAP>00000</CAP>
        <Comune>Ta Xbiex</Comune>
        <Nazione>MT</Nazione>
      </Sede>
    </CessionarioCommittente>
  </FatturaElettronicaHeader>
  <FatturaElettronicaBody xmlns="">
    <DatiGenerali>
      <DatiGeneraliDocumento>
        <TipoDocumento>TD01</TipoDocumento>
        <Divisa>EUR</Divisa>
        <Data>2026-01-14</Data>
        <Numero>FPR 1/26</Numero>
        <ImportoTotaleDocumento>5000.00</ImportoTotaleDocumento>
      </DatiGeneraliDocumento>
    </DatiGenerali>
    <DatiBeniServizi>
      <DettaglioLinee>
        <NumeroLinea>1</NumeroLinea>
        <Descrizione>Consulenza</Descrizione>
        <Quantita>1.00</Quantita>
        <PrezzoUnitario>5000.00</PrezzoUnitario>
        <PrezzoTotale>5000.00</PrezzoTotale>
        <AliquotaIVA>0.00</AliquotaIVA>
        <Natura>N2.1</Natura>
      </DettaglioLinee>
      <DatiRiepilogo>
        <AliquotaIVA>0.00</AliquotaIVA>
        <Natura>N2.1</Natura>
        <ImponibileImporto>5000.00</ImponibileImporto>
        <Imposta>0.00</Imposta>
        <RiferimentoNormativo>Non soggette ad IVA ai sensi degli artt. Da 7 a 7-septies del DPR 633/72</RiferimentoNormativo>
      </DatiRiepilogo>
    </DatiBeniServizi>
  </FatturaElettronicaBody>
</FatturaElettronica>
""";

    [Fact]
    public void Parse_Sample_Invoice_Extracts_Header_And_Body()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SampleXml));

        var dto = FatturaElettronicaParser.Parse(stream);

        dto.Cedente.Denominazione.Should().Be("Jadis Consulting SRL");
        dto.Cedente.PartitaIva.Should().Be("14450591004");
        dto.Cedente.PaeseIva.Should().Be("IT");
        dto.Cedente.Email.Should().Be("amministrazione@jadisconsulting.com");
        dto.Cedente.Comune.Should().Be("Roma");

        dto.Cessionario.Denominazione.Should().Be("Omniversity Edutech LTD");
        dto.Cessionario.PartitaIva.Should().Be("MT31185926");
        dto.Cessionario.PaeseIva.Should().Be("MT");
        dto.Cessionario.Nazione.Should().Be("MT");

        dto.Data.Should().Be(new DateOnly(2026, 1, 14));
        dto.Numero.Should().Be("FPR 1/26");
        dto.ImportoTotale.Should().Be(5000.00m);

        dto.Riepilogo.Should().HaveCount(1);
        var riep = dto.Riepilogo[0];
        riep.AliquotaPercentuale.Should().Be(0m);
        riep.Natura.Should().Be("N2.1");
        riep.Imponibile.Should().Be(5000.00m);
        riep.Imposta.Should().Be(0m);
        riep.Totale.Should().Be(5000.00m);
    }

    [Fact]
    public void Parse_Invalid_Xml_Throws()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not xml"));
        var act = () => FatturaElettronicaParser.Parse(stream);
        act.Should().Throw<FatturaElettronicaParseException>();
    }

    [Fact]
    public void Parse_Missing_Root_Throws()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><Wrong/>"));
        var act = () => FatturaElettronicaParser.Parse(stream);
        act.Should().Throw<FatturaElettronicaParseException>();
    }
}
