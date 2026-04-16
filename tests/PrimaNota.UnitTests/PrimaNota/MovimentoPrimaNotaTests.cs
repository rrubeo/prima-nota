using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.UnitTests.PrimaNota;

public sealed class MovimentoPrimaNotaTests
{
    private static readonly Guid CausaleId = Guid.NewGuid();
    private static readonly Guid Conto1 = Guid.NewGuid();
    private static readonly Guid Conto2 = Guid.NewGuid();
    private static readonly Guid Categoria1 = Guid.NewGuid();
    private static readonly Guid Categoria2 = Guid.NewGuid();

    [Fact]
    public void Constructor_Should_Start_In_Draft_With_Empty_Lines()
    {
        var mov = NewDraft();

        mov.Stato.Should().Be(StatoMovimento.Draft);
        mov.Righe.Should().BeEmpty();
        mov.Allegati.Should().BeEmpty();
        mov.Totale.Should().Be(0m);
    }

    [Fact]
    public void Constructor_Should_Reject_Date_Outside_Fiscal_Year()
    {
        var act = () => new MovimentoPrimaNota(new DateOnly(2025, 12, 31), 2026, "desc", CausaleId);
        act.Should().Throw<ArgumentException>().WithMessage("*esercizio*");
    }

    [Fact]
    public void ReplaceRighe_Should_Require_At_Least_One_Line()
    {
        var mov = NewDraft();
        var act = () => mov.ReplaceRighe(Array.Empty<RigaMovimento>());
        act.Should().Throw<InvalidOperationException>().WithMessage("*una riga*");
    }

    [Fact]
    public void Confirm_Single_Line_Movement_Should_Succeed()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(100m, Conto1, Categoria1) });

        mov.Confirm();

        mov.Stato.Should().Be(StatoMovimento.Confirmed);
    }

    [Fact]
    public void Confirm_MultiAccount_Unbalanced_Movement_Should_Throw()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[]
        {
            new RigaMovimento(100m, Conto1, Categoria1),
            new RigaMovimento(-50m, Conto2, Categoria2),
        });

        var act = mov.Confirm;

        act.Should().Throw<InvalidOperationException>().WithMessage("*pareggio*");
    }

    [Fact]
    public void Confirm_Balanced_Giroconto_Should_Succeed()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[]
        {
            new RigaMovimento(-500m, Conto1, Categoria1),
            new RigaMovimento(500m, Conto2, Categoria1),
        });

        mov.Confirm();

        mov.IsGiroconto.Should().BeTrue();
        mov.Totale.Should().Be(0m);
    }

    [Fact]
    public void Split_Multiple_Lines_Same_Account_Is_Not_Giroconto()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[]
        {
            new RigaMovimento(-200m, Conto1, Categoria1),
            new RigaMovimento(-300m, Conto1, Categoria2),
        });

        mov.Confirm();

        mov.IsGiroconto.Should().BeFalse();
        mov.Totale.Should().Be(-500m);
    }

    [Fact]
    public void UpdateHeader_On_Confirmed_Movement_Should_Throw()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(100m, Conto1, Categoria1) });
        mov.Confirm();

        var act = () => mov.UpdateHeader(new DateOnly(2026, 5, 2), "new", CausaleId, null, null, null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Draft*");
    }

    [Fact]
    public void Unconfirm_From_Confirmed_Should_Return_To_Draft()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(100m, Conto1, Categoria1) });
        mov.Confirm();

        mov.Unconfirm();

        mov.Stato.Should().Be(StatoMovimento.Draft);
    }

    [Fact]
    public void MarkReconciled_From_Confirmed_Should_Succeed()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(100m, Conto1, Categoria1) });
        mov.Confirm();

        mov.MarkReconciled();

        mov.Stato.Should().Be(StatoMovimento.Reconciled);
    }

    [Fact]
    public void MarkReconciled_From_Draft_Should_Throw()
    {
        var mov = NewDraft();
        var act = mov.MarkReconciled;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unconfirm_From_Reconciled_Should_Throw()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(100m, Conto1, Categoria1) });
        mov.Confirm();
        mov.MarkReconciled();

        var act = mov.Unconfirm;

        act.Should().Throw<InvalidOperationException>().WithMessage("*riconciliato*");
    }

    [Fact]
    public void AddAllegato_On_Reconciled_Should_Throw()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(100m, Conto1, Categoria1) });
        mov.Confirm();
        mov.MarkReconciled();

        var act = () => mov.AddAllegato(new Allegato(
            "x.pdf",
            "application/pdf",
            10,
            new string('0', 64),
            "movimenti/2026/x.pdf",
            DateTimeOffset.UtcNow,
            null));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPagamento_Should_Reduce_Residuo_And_Update_DataPagamento()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(-1000m, Conto1, Categoria1) });
        mov.Confirm();

        mov.Residuo.Should().Be(1000m);
        mov.IsFullyPaid.Should().BeFalse();
        mov.DataPagamento.Should().BeNull();

        mov.AddPagamento(new PagamentoMovimento(new DateOnly(2026, 1, 15), 300m, Conto2));

        mov.TotalePagato.Should().Be(300m);
        mov.Residuo.Should().Be(700m);
        mov.IsFullyPaid.Should().BeFalse();
        mov.DataPagamento.Should().BeNull();

        mov.AddPagamento(new PagamentoMovimento(new DateOnly(2026, 2, 28), 700m, Conto2));

        mov.TotalePagato.Should().Be(1000m);
        mov.Residuo.Should().Be(0m);
        mov.IsFullyPaid.Should().BeTrue();
        mov.DataPagamento.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void AddPagamento_OverPayment_Should_Throw()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(-100m, Conto1, Categoria1) });
        mov.Confirm();

        var act = () => mov.AddPagamento(new PagamentoMovimento(new DateOnly(2026, 1, 15), 150m, Conto2));

        act.Should().Throw<InvalidOperationException>().WithMessage("*supera il residuo*");
    }

    [Fact]
    public void AddPagamento_On_Reconciled_Should_Throw()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(-100m, Conto1, Categoria1) });
        mov.Confirm();
        mov.MarkReconciled();

        var act = () => mov.AddPagamento(new PagamentoMovimento(new DateOnly(2026, 1, 15), 50m, Conto2));

        act.Should().Throw<InvalidOperationException>().WithMessage("*riconciliato*");
    }

    [Fact]
    public void RemovePagamento_Should_Restore_Residuo()
    {
        var mov = NewDraft();
        mov.ReplaceRighe(new[] { new RigaMovimento(-500m, Conto1, Categoria1) });
        mov.Confirm();
        var pag = new PagamentoMovimento(new DateOnly(2026, 1, 15), 500m, Conto2);
        mov.AddPagamento(pag);

        mov.IsFullyPaid.Should().BeTrue();
        var removed = mov.RemovePagamento(pag.Id);

        removed.Should().NotBeNull();
        mov.Residuo.Should().Be(500m);
        mov.IsFullyPaid.Should().BeFalse();
        mov.DataPagamento.Should().BeNull();
    }

    [Fact]
    public void PagamentoMovimento_With_Non_Positive_Importo_Should_Throw()
    {
        var act = () => new PagamentoMovimento(new DateOnly(2026, 1, 1), 0m, Conto1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RigaMovimento_With_Zero_Importo_Should_Throw()
    {
        var act = () => new RigaMovimento(0m, Conto1, Categoria1);
        act.Should().Throw<ArgumentException>().WithMessage("*zero*");
    }

    [Fact]
    public void Allegato_With_Wrong_Hash_Length_Should_Throw()
    {
        var act = () => new Allegato(
            "x.pdf",
            "application/pdf",
            10,
            "short",
            "x",
            DateTimeOffset.UtcNow,
            null);
        act.Should().Throw<ArgumentException>().WithMessage("*SHA-256*");
    }

    private static MovimentoPrimaNota NewDraft() =>
        new(new DateOnly(2026, 5, 1), 2026, "test", CausaleId);
}
