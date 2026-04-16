using FluentValidation;

namespace PrimaNota.Application.PrimaNota;

/// <summary>Payload for create/update of a prima-nota movement.</summary>
public sealed class MovimentoInput
{
    /// <summary>Gets or sets the movement date.</summary>
    public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Gets or sets the fiscal year (inferred from Data when creating).</summary>
    public int EsercizioAnno { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Descrizione { get; set; } = string.Empty;

    /// <summary>Gets or sets the reference number.</summary>
    public string? Numero { get; set; }

    /// <summary>Gets or sets the causale id.</summary>
    public Guid CausaleId { get; set; }

    /// <summary>Gets or sets the default counterparty.</summary>
    public Guid? AnagraficaId { get; set; }

    /// <summary>Gets or sets the free-form notes.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets the lines (at least one required).</summary>
    public List<RigaMovimentoInput> Righe { get; set; } = new();
}

/// <summary>Line payload.</summary>
public sealed class RigaMovimentoInput
{
    /// <summary>Gets or sets the line id (empty for new lines).</summary>
    public Guid? Id { get; set; }

    /// <summary>Gets or sets the signed amount.</summary>
    public decimal Importo { get; set; }

    /// <summary>Gets or sets the financial account.</summary>
    public Guid ContoFinanziarioId { get; set; }

    /// <summary>Gets or sets the category.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Gets or sets the counterparty.</summary>
    public Guid? AnagraficaId { get; set; }

    /// <summary>Gets or sets the VAT rate.</summary>
    public Guid? AliquotaIvaId { get; set; }

    /// <summary>Gets or sets the optional note.</summary>
    public string? Note { get; set; }
}

/// <summary>Validator for <see cref="MovimentoInput"/>.</summary>
public sealed class MovimentoInputValidator : AbstractValidator<MovimentoInput>
{
    /// <summary>Initializes a new instance of the <see cref="MovimentoInputValidator"/> class.</summary>
    public MovimentoInputValidator()
    {
        RuleFor(x => x.Descrizione).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CausaleId).NotEmpty().WithMessage("Causale obbligatoria.");
        RuleFor(x => x.Numero).MaximumLength(64);
        RuleFor(x => x.Note).MaximumLength(2000);
        RuleFor(x => x.EsercizioAnno).GreaterThanOrEqualTo(2000);
        RuleFor(x => x).Must(x => x.Data.Year == x.EsercizioAnno)
            .WithMessage("La data deve appartenere all'esercizio selezionato.");
        RuleFor(x => x.Righe).NotEmpty().WithMessage("Almeno una riga e obbligatoria.");
        RuleForEach(x => x.Righe).SetValidator(new RigaMovimentoInputValidator());

        When(x => x.Righe.Select(r => r.ContoFinanziarioId).Distinct().Count() >= 2, () =>
        {
            RuleFor(x => x.Righe.Sum(r => r.Importo))
                .Equal(0m)
                .WithMessage("I movimenti a piu conti (giroconto) devono avere somma zero.");
        });
    }
}

/// <summary>Validator for <see cref="RigaMovimentoInput"/>.</summary>
public sealed class RigaMovimentoInputValidator : AbstractValidator<RigaMovimentoInput>
{
    /// <summary>Initializes a new instance of the <see cref="RigaMovimentoInputValidator"/> class.</summary>
    public RigaMovimentoInputValidator()
    {
        RuleFor(x => x.Importo).NotEqual(0m).WithMessage("Importo diverso da zero.");
        RuleFor(x => x.ContoFinanziarioId).NotEmpty();
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
