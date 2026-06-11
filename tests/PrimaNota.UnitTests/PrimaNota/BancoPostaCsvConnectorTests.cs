using PrimaNota.Infrastructure.BankStatements;

namespace PrimaNota.UnitTests.PrimaNota;

public sealed class BancoPostaCsvConnectorTests
{
    private static readonly BancoPostaCsvConnector Connector = new();

    // Tab-separated BancoPosta "Saldo e Movimenti" export, rebuilt from a real sample.
    // Empty amount cells are a single space, exactly as Poste exports them.
    private static readonly string SampleCsv = BuildSample();

    [Fact]
    public void CanParse_RecognizesBancoPostaCsv()
    {
        Connector.CanParse("estratto.csv", SampleCsv).Should().BeTrue();
    }

    [Fact]
    public void CanParse_RejectsNonCsvExtension()
    {
        Connector.CanParse("estratto.pdf", SampleCsv).Should().BeFalse();
    }

    [Fact]
    public void CanParse_RejectsUnknownCsv()
    {
        Connector.CanParse("altro.csv", "col1;col2\n1;2").Should().BeFalse();
    }

    [Fact]
    public void Parse_ExtractsAllMovements()
    {
        var result = Connector.Parse(SampleCsv);

        result.Righe.Should().HaveCount(28);
    }

    [Fact]
    public void Parse_ExtractsPeriodFromFilters()
    {
        var result = Connector.Parse(SampleCsv);

        result.PeriodoDa.Should().Be(new DateOnly(2026, 1, 1));
        result.PeriodoA.Should().Be(new DateOnly(2026, 1, 31));
    }

    [Fact]
    public void Parse_ExtractsAccountingBalance()
    {
        var result = Connector.Parse(SampleCsv);

        result.SaldoContabile.Should().Be(41657.41m);
    }

    [Fact]
    public void Parse_MapsCreditAsPositiveAmount()
    {
        var result = Connector.Parse(SampleCsv);

        var credit = result.Righe.Single(r => r.Importo == 488.00m);
        credit.DataContabile.Should().Be(new DateOnly(2026, 1, 29));
        credit.CausaleOperazione.Should().Be("48");
        credit.Operazione.Should().Be("BONIFICO SEPA");
        credit.Descrizione.Should().Contain("SIRIO SERVICE");
    }

    [Fact]
    public void Parse_MapsDebitAsNegativeAmount()
    {
        var result = Connector.Parse(SampleCsv);

        var debit = result.Righe.Single(r => r.Importo == -2000.00m);
        debit.Operazione.Should().Be("POSTAGIRO");
        debit.CausaleOperazione.Should().Be("PO");
    }

    [Fact]
    public void Parse_KeepsSignsConsistent()
    {
        var result = Connector.Parse(SampleCsv);

        // One inbound bonifico + nine cashback credits = ten positive rows; the rest are debits.
        result.Righe.Count(r => r.Importo > 0).Should().Be(10);
        result.Righe.Count(r => r.Importo < 0).Should().Be(18);
    }

    [Fact]
    public void Parse_SupportsSemicolonDelimiter()
    {
        var semicolon = SampleCsv.Replace('\t', ';');

        var result = Connector.Parse(semicolon);

        result.Righe.Should().HaveCount(28);
        result.SaldoContabile.Should().Be(41657.41m);
    }

    private static string BuildSample()
    {
        const string blank = "";
        string Row(params string[] cells) => string.Join('\t', cells);
        string Credit(string day, string amount, string causale, string descr, string op) =>
            Row($"{day}/01/2026", $"{day}/01/2026", " ", amount, causale, descr, op);
        string Debit(string day, string amount, string causale, string descr, string op) =>
            Row($"{day}/01/2026", $"{day}/01/2026", amount, " ", causale, descr, op);

        var lines = new[]
        {
            "SALDO E MOVIMENTI",
            blank,
            blank,
            "RIEPILOGO CONTO CORRENTE",
            Row("Numero Conto corrente BancoPosta", "Intestato a", "Saldo al", "Saldo contabile conto", "Saldo disponibile conto"),
            Row("1044615100", "JADIS CONSULTING SRLS", "17/04/2026", "41657,41", "41657,41", " "),
            blank,
            blank,
            "FILTRI DI RICERCA",
            Row("Data inizio", "Data fine", "Tipo operazione", "Causale operazione"),
            Row("01/01/2026", "31/01/2026", "TUTTE", " "),
            blank,
            blank,
            "LISTA MOVIMENTI ",
            Row("Data contabile", "Data valuta", "Addebito", "Accredito", "Causale operazione", "Operazione", "Descrizione movimento"),
            Credit("29", "488,00", "48", "Da SIRIO SERVICE 2004 SOCIETA' COOPERATIVA per SALDO FT. 2/26", "BONIFICO SEPA"),
            Debit("23", "-52,00", "26", "Distinta: 176911272442716O8DEF", "BONIFICO SEPA"),
            Debit("20", "-3600,00", "26", "Distinta: 176883452057707BADEF", "BONIFICO SEPA"),
            Debit("20", "-2000,00", "PO", "A RUBEO ROMOLO per restituzione finanziamento non fruttifero", "POSTAGIRO"),
            Debit("19", "-1000,00", "380PRIRIC", "Addebito Conto per Ricarica Postepay eseguita da BPIOL", "RICARICA POSTEPAY"),
            Debit("19", "-1,00", "160PRIRIC", "Addebito Conto per Ricarica Postepay eseguita da BPIOL", "COMMISSIONI RICARICA POSTEPAY"),
            Debit("19", "-900,00", "1902I", "MODELLO F24 - ADDEBITO DELEGA CF 14450591004", "F24 BPIOL"),
            Credit("17", "1,10", "36REBA", "PER ACQUISTO DI EURO 220,56 DA AMZNBusiness", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,35", "36REBA", "PER ACQUISTO DI EURO 69,99 DA AMZNBusiness", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,16", "36REBA", "PER ACQUISTO DI EURO 32,07 DA COOP", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,15", "36REBA", "PER ACQUISTO DI EURO 30,36 DA AMAZON", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,15", "36REBA", "PER ACQUISTO DI EURO 29,98 DA 00148 - SCURCOLA", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,12", "36REBA", "PER ACQUISTO DI EURO 24,98 DA IPER ONE STORE", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,06", "36REBA", "PER ACQUISTO DI EURO 11,39 DA AMAZON", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,05", "36REBA", "PER ACQUISTO DI EURO 9,59 DA AMZNBusiness", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Credit("17", "0,04", "36REBA", "PER ACQUISTO DI EURO 8,99 DA AMAZON", "ACCREDITO PER POSTEPAY CASHBACK BUSINESS"),
            Debit("16", "-1042,68", "26", "Distinta: 176849436518319FADEF", "BONIFICO SEPA"),
            Debit("15", "-5000,00", "26", "Distinta: 17684019080720TOLDEF", "BONIFICO SEPA"),
            Debit("14", "-5,77", "26", "Distinta: 17683143571790IJ3DEF", "BONIFICO SEPA"),
            Debit("07", "-1500,00", "26", "Distinta: 17677865497333A4EDEF", "BONIFICO SEPA"),
            Debit("05", "-360,00", "26", "Distinta: 17676059282862UE4DEF", "BONIFICO SEPA"),
            Debit("05", "-10,00", "1606", "ADDEBITO RELATIVO AL PERIODO DI DICEMBRE 2025", "TENUTA CONTO"),
            Debit("05", "-5,00", "0516", "ADDEBITO CANONE DEL SERVIZIO BANCOPOSTAIMPRESA ONLINE", "CANONE SERVIZIO COLLEGAMENTO TELEMATICO"),
            Debit("05", "-0,40", "16ADD", "ARVAL SERVICE LEASE", "COMMISSIONI DOMICILIAZIONE (ADDEBITO DIRETTO SEPA)"),
            Debit("03", "-8,99", "043", "AMAZON 31/12/2025 09.54 LUXEMBOURG carta ****4159", "PAGAMENTO POS"),
            Debit("03", "-100,00", "019", " ", "IMPOSTA DI BOLLO"),
            Debit("02", "-326,90", "151F", "ARVAL SERVICE LEASE", "RATA FINANZIAMENTO"),
            Debit("02", "-11,39", "043", "AMAZON 30/12/2025 16.45 LUXEMBOURG carta ****4159", "PAGAMENTO POS"),
        };

        return string.Join('\n', lines);
    }
}
