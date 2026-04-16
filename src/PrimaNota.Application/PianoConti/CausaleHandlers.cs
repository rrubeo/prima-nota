using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PianoConti;

namespace PrimaNota.Application.PianoConti;

/// <summary>DTO for causali lists and forms.</summary>
public sealed record CausaleDto(
    Guid Id,
    string Codice,
    string Nome,
    TipoMovimento Tipo,
    Guid? CategoriaDefaultId,
    string? CategoriaDefaultNome,
    bool Attiva,
    string? Note);

/// <summary>Payload for create/update.</summary>
public sealed class CausaleInput
{
    /// <summary>Gets or sets the code.</summary>
    public string Codice { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation kind.</summary>
    public TipoMovimento Tipo { get; set; } = TipoMovimento.Pagamento;

    /// <summary>Gets or sets the default category id.</summary>
    public Guid? CategoriaDefaultId { get; set; }

    /// <summary>Gets or sets the notes.</summary>
    public string? Note { get; set; }
}

/// <summary>Validator for <see cref="CausaleInput"/>.</summary>
public sealed class CausaleInputValidator : AbstractValidator<CausaleInput>
{
    /// <summary>Initializes a new instance of the <see cref="CausaleInputValidator"/> class.</summary>
    public CausaleInputValidator()
    {
        RuleFor(x => x.Codice).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

/// <summary>Lists causali.</summary>
/// <param name="IncludiNonAttive">Include inactive rows.</param>
public sealed record ListCausali(bool IncludiNonAttive = false) : IRequest<IReadOnlyList<CausaleDto>>;

/// <summary>Creates a causale.</summary>
/// <param name="Input">Payload.</param>
public sealed record CreateCausale(CausaleInput Input) : IRequest<Guid>;

/// <summary>Updates a causale.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Input">Payload.</param>
public sealed record UpdateCausale(Guid Id, CausaleInput Input) : IRequest;

/// <summary>Toggles the active flag.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Attiva">Desired state.</param>
public sealed record ToggleCausaleActivation(Guid Id, bool Attiva) : IRequest;

/// <summary>Handles <see cref="ListCausali"/>.</summary>
public sealed class ListCausaliHandler : IRequestHandler<ListCausali, IReadOnlyList<CausaleDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListCausaliHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public ListCausaliHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CausaleDto>> Handle(ListCausali request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = from c in db.Causali.AsNoTracking()
                    join cat in db.Categorie.AsNoTracking() on c.CategoriaDefaultId equals cat.Id into j
                    from cat in j.DefaultIfEmpty()
                    where request.IncludiNonAttive || c.Attiva
                    orderby c.Tipo, c.Codice
                    select new CausaleDto(
                        c.Id,
                        c.Codice,
                        c.Nome,
                        c.Tipo,
                        c.CategoriaDefaultId,
                        cat != null ? cat.Nome : null,
                        c.Attiva,
                        c.Note);

        return await query.ToListAsync(cancellationToken);
    }
}

/// <summary>Handles <see cref="CreateCausale"/>.</summary>
public sealed class CreateCausaleHandler : IRequestHandler<CreateCausale, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="CreateCausaleHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public CreateCausaleHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateCausale request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Causale(request.Input.Codice, request.Input.Nome, request.Input.Tipo);
        entity.Update(
            request.Input.Codice,
            request.Input.Nome,
            request.Input.Tipo,
            request.Input.CategoriaDefaultId,
            request.Input.Note);
        db.Causali.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Validator for create.</summary>
public sealed class CreateCausaleValidator : AbstractValidator<CreateCausale>
{
    /// <summary>Initializes a new instance of the <see cref="CreateCausaleValidator"/> class.</summary>
    public CreateCausaleValidator()
    {
        RuleFor(x => x.Input).NotNull().SetValidator(new CausaleInputValidator());
    }
}

/// <summary>Handles <see cref="UpdateCausale"/>.</summary>
public sealed class UpdateCausaleHandler : IRequestHandler<UpdateCausale>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateCausaleHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public UpdateCausaleHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateCausale request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Causali.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Causale {request.Id} non trovata.");

        entity.Update(
            request.Input.Codice,
            request.Input.Nome,
            request.Input.Tipo,
            request.Input.CategoriaDefaultId,
            request.Input.Note);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Validator for update.</summary>
public sealed class UpdateCausaleValidator : AbstractValidator<UpdateCausale>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateCausaleValidator"/> class.</summary>
    public UpdateCausaleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Input).NotNull().SetValidator(new CausaleInputValidator());
    }
}

/// <summary>Handles <see cref="ToggleCausaleActivation"/>.</summary>
public sealed class ToggleCausaleActivationHandler : IRequestHandler<ToggleCausaleActivation>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ToggleCausaleActivationHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public ToggleCausaleActivationHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(ToggleCausaleActivation request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Causali.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Causale {request.Id} non trovata.");
        entity.SetAttiva(request.Attiva);
        await db.SaveChangesAsync(cancellationToken);
    }
}
