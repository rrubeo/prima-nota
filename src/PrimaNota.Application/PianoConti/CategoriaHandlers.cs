using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PianoConti;

namespace PrimaNota.Application.PianoConti;

/// <summary>DTO for categorie lists and forms.</summary>
public sealed record CategoriaDto(
    Guid Id,
    string Codice,
    string Nome,
    NaturaCategoria Natura,
    bool Attiva,
    string? Note);

/// <summary>Payload for create/update.</summary>
public sealed class CategoriaInput
{
    /// <summary>Gets or sets the code.</summary>
    public string Codice { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Gets or sets the nature.</summary>
    public NaturaCategoria Natura { get; set; } = NaturaCategoria.Uscita;

    /// <summary>Gets or sets the notes.</summary>
    public string? Note { get; set; }
}

/// <summary>Validator for <see cref="CategoriaInput"/>.</summary>
public sealed class CategoriaInputValidator : AbstractValidator<CategoriaInput>
{
    /// <summary>Initializes a new instance of the <see cref="CategoriaInputValidator"/> class.</summary>
    public CategoriaInputValidator()
    {
        RuleFor(x => x.Codice).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

/// <summary>Lists categorie.</summary>
/// <param name="IncludiNonAttive">Include inactive rows.</param>
public sealed record ListCategorie(bool IncludiNonAttive = false) : IRequest<IReadOnlyList<CategoriaDto>>;

/// <summary>Creates a categoria.</summary>
/// <param name="Input">Payload.</param>
public sealed record CreateCategoria(CategoriaInput Input) : IRequest<Guid>;

/// <summary>Updates a categoria.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Input">Payload.</param>
public sealed record UpdateCategoria(Guid Id, CategoriaInput Input) : IRequest;

/// <summary>Toggles the active flag of a categoria.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Attiva">Desired state.</param>
public sealed record ToggleCategoriaActivation(Guid Id, bool Attiva) : IRequest;

/// <summary>Handles <see cref="ListCategorie"/>.</summary>
public sealed class ListCategorieHandler : IRequestHandler<ListCategorie, IReadOnlyList<CategoriaDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListCategorieHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public ListCategorieHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoriaDto>> Handle(ListCategorie request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var query = db.Categorie.AsNoTracking().AsQueryable();
        if (!request.IncludiNonAttive)
        {
            query = query.Where(c => c.Attiva);
        }

        return await query
            .OrderBy(c => c.Natura)
            .ThenBy(c => c.Codice)
            .Select(c => new CategoriaDto(c.Id, c.Codice, c.Nome, c.Natura, c.Attiva, c.Note))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Handles <see cref="CreateCategoria"/>.</summary>
public sealed class CreateCategoriaHandler : IRequestHandler<CreateCategoria, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="CreateCategoriaHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public CreateCategoriaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateCategoria request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Categoria(request.Input.Codice, request.Input.Nome, request.Input.Natura);
        entity.Update(request.Input.Codice, request.Input.Nome, request.Input.Natura, request.Input.Note);
        db.Categorie.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Validator for create.</summary>
public sealed class CreateCategoriaValidator : AbstractValidator<CreateCategoria>
{
    /// <summary>Initializes a new instance of the <see cref="CreateCategoriaValidator"/> class.</summary>
    public CreateCategoriaValidator()
    {
        RuleFor(x => x.Input).NotNull().SetValidator(new CategoriaInputValidator());
    }
}

/// <summary>Handles <see cref="UpdateCategoria"/>.</summary>
public sealed class UpdateCategoriaHandler : IRequestHandler<UpdateCategoria>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateCategoriaHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public UpdateCategoriaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateCategoria request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Categorie.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Categoria {request.Id} non trovata.");

        entity.Update(request.Input.Codice, request.Input.Nome, request.Input.Natura, request.Input.Note);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Validator for update.</summary>
public sealed class UpdateCategoriaValidator : AbstractValidator<UpdateCategoria>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateCategoriaValidator"/> class.</summary>
    public UpdateCategoriaValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Input).NotNull().SetValidator(new CategoriaInputValidator());
    }
}

/// <summary>Handles <see cref="ToggleCategoriaActivation"/>.</summary>
public sealed class ToggleCategoriaActivationHandler : IRequestHandler<ToggleCategoriaActivation>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ToggleCategoriaActivationHandler"/> class.</summary>
    /// <param name="db">DB context.</param>
    public ToggleCategoriaActivationHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(ToggleCategoriaActivation request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Categorie.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Categoria {request.Id} non trovata.");
        entity.SetAttiva(request.Attiva);
        await db.SaveChangesAsync(cancellationToken);
    }
}
