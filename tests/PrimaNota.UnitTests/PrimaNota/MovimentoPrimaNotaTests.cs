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
