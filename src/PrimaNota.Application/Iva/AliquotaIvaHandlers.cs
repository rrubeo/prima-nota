using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Application.Iva;

/// <summary>DTO for aliquote IVA lists and forms.</summary>
public sealed record AliquotaIvaDto(
    Guid Id,
    string Codice,
    string Descrizione,
    decimal Percentuale,
    decimal PercentualeIndetraibile,
    TipoIva Tipo,
    string? CodiceNatura,
    bool Attiva);

/// <summary>Payload for create/update.</summary>
public sealed class AliquotaIvaInput
{
    /// <summary>Gets or sets the code.</summary>
    public string Codice { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Descrizione { get; set; } = string.Empty;

    /// <summary>Gets or sets the percentage.</summary>
    public decimal Percentuale { get; set; }

    /// <summary>Gets or sets the non-deductible percentage.</summary>
    public decimal PercentualeIndetraibile { get; set; }

    /// <summary>Gets or sets the VAT treatment.</summary>
    public TipoIva Tipo { get; set; } = TipoIva.Ordinaria;

    /// <summary>Gets or sets the "natura" code.</summary>
    public string? CodiceNatura { get; set; }
}

/// <summary>Validator for <see cref="AliquotaIvaInput"/>.</summary>
public sealed class AliquotaIvaInputValidator : AbstractValidator<AliquotaIvaInput>
{
    /// <summary>Initializes a new instance of the <see cref="AliquotaIvaInputValidator"/> class.</summary>
    public AliquotaIvaInputValidator()
    {
        RuleFor(x => x.Codice).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Descrizione).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Percentuale).InclusiveBetween(0m, 100m);
        RuleFor(x => x.PercentualeIndetraibile).InclusiveBetween(0m, 100m);
        RuleFor(x => x.CodiceNatura).MaximumLength(8);

        When(x => x.Tipo != TipoIva.Ordinaria, () =>
        {
            RuleFor(x => x.Percentuale).Equal(0m)
                .WithMessage("Solo le aliquote ordinarie possono avere una percentuale > 0.");
        });
    }
}

/// <summary>Lists aliquote IVA.</summary>
/// <param name="IncludiNonAttive">Include inactive rows.</param>
public sealed record ListAliquoteIva(bool IncludiNonAttive = false) : IRequest<IReadOnlyList<AliquotaIvaDto>>;

/// <summary>Creates an aliquota IVA.</summary>
/// <param name="Input">Payload.</param>
public sealed record CreateAliquotaIva(AliquotaIvaInput Input) : IRequest<Guid>;

/// <summary>Updates an aliquota IVA.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Input">Payload.</param>
public sealed record UpdateAliquotaIva(Guid Id, AliquotaIvaInput Input) : IRequest;

/// <summary>Toggles the active flag.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Attiva">Desired state.</param>
public sealed record ToggleAliquotaIvaActivation(Guid Id, bool Attiva) : IRequest;

/// <summary>Handles <see cref="ListAliquoteIva"/>.</summary>
public sealed class ListAliquoteIvaHandler : IRequestHandler<ListAliquoteIva, IReadOnlyList<AliquotaIvaDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListAliquoteIvaHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public ListAliquoteIvaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AliquotaIvaDto>> Handle(ListAliquoteIva request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var query = db.AliquoteIva.AsNoTracking().AsQueryable();
        if (!request.IncludiNonAttive)
        {
            query = query.Where(a => a.Attiva);
        }

        return await query
            .OrderBy(a => a.Tipo)
            .ThenByDescending(a => a.Percentuale)
            .ThenBy(a => a.Codice)
            .Select(a => new AliquotaIvaDto(a.Id, a.Codice, a.Descrizione, a.Percentuale, a.PercentualeIndetraibile, a.Tipo, a.CodiceNatura, a.Attiva))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Handles <see cref="CreateAliquotaIva"/>.</summary>
public sealed class CreateAliquotaIvaHandler : IRequestHandler<CreateAliquotaIva, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="CreateAliquotaIvaHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public CreateAliquotaIvaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateAliquotaIva request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new AliquotaIva(request.Input.Codice, request.Input.Descrizione, request.Input.Percentuale, request.Input.Tipo);
        entity.Update(
            request.Input.Codice,
            request.Input.Descrizione,
            request.Input.Percentuale,
            request.Input.PercentualeIndetraibile,
            request.Input.Tipo,
            request.Input.CodiceNatura);
        db.AliquoteIva.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Validator for create.</summary>
public sealed class CreateAliquotaIvaValidator : AbstractValidator<CreateAliquotaIva>
{
    /// <summary>Initializes a new instance of the <see cref="CreateAliquotaIvaValidator"/> class.</summary>
    public CreateAliquotaIvaValidator()
    {
        RuleFor(x => x.Input).NotNull().SetValidator(new AliquotaIvaInputValidator());
    }
}

/// <summary>Handles <see cref="UpdateAliquotaIva"/>.</summary>
public sealed class UpdateAliquotaIvaHandler : IRequestHandler<UpdateAliquotaIva>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateAliquotaIvaHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public UpdateAliquotaIvaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateAliquotaIva request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.AliquoteIva.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Aliquota IVA {request.Id} non trovata.");

        entity.Update(
            request.Input.Codice,
            request.Input.Descrizione,
            request.Input.Percentuale,
            request.Input.PercentualeIndetraibile,
            request.Input.Tipo,
            request.Input.CodiceNatura);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Validator for update.</summary>
public sealed class UpdateAliquotaIvaValidator : AbstractValidator<UpdateAliquotaIva>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateAliquotaIvaValidator"/> class.</summary>
    public UpdateAliquotaIvaValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Input).NotNull().SetValidator(new AliquotaIvaInputValidator());
    }
}

/// <summary>Handles <see cref="ToggleAliquotaIvaActivation"/>.</summary>
public sealed class ToggleAliquotaIvaActivationHandler : IRequestHandler<ToggleAliquotaIvaActivation>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ToggleAliquotaIvaActivationHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public ToggleAliquotaIvaActivationHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(ToggleAliquotaIvaActivation request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.AliquoteIva.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Aliquota IVA {request.Id} non trovata.");
        entity.SetAttiva(request.Attiva);
        await db.SaveChangesAsync(cancellationToken);
    }
}
