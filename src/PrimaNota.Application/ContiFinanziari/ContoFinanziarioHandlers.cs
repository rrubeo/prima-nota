using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.ContiFinanziari;

namespace PrimaNota.Application.ContiFinanziari;

/// <summary>Full DTO for ContoFinanziario.</summary>
public sealed record ContoFinanziarioDto(
    Guid Id,
    string Codice,
    string Nome,
    TipoConto Tipo,
    string? Istituto,
    string? Iban,
    string? Bic,
    string? Intestatario,
    string? Ultime4Cifre,
    decimal SaldoIniziale,
    DateOnly DataSaldoIniziale,
    decimal SaldoCorrente,
    string Valuta,
    bool Attivo,
    string? Note);

/// <summary>Compact projection for the list page, includes live balance.</summary>
public sealed record ContoFinanziarioListItemDto(
    Guid Id,
    string Codice,
    string Nome,
    TipoConto Tipo,
    string? Istituto,
    string? Iban,
    string? Ultime4Cifre,
    decimal SaldoCorrente,
    string Valuta,
    bool Attivo);

/// <summary>Editable payload.</summary>
public sealed class ContoFinanziarioInput
{
    /// <summary>Gets or sets the code.</summary>
    public string Codice { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind.</summary>
    public TipoConto Tipo { get; set; } = TipoConto.Banca;

    /// <summary>Gets or sets the issuer / bank name.</summary>
    public string? Istituto { get; set; }

    /// <summary>Gets or sets the IBAN.</summary>
    public string? Iban { get; set; }

    /// <summary>Gets or sets the BIC/SWIFT.</summary>
    public string? Bic { get; set; }

    /// <summary>Gets or sets the cardholder name.</summary>
    public string? Intestatario { get; set; }

    /// <summary>Gets or sets the last four digits of the card number.</summary>
    public string? Ultime4Cifre { get; set; }

    /// <summary>Gets or sets the opening balance.</summary>
    public decimal SaldoIniziale { get; set; }

    /// <summary>Gets or sets the opening-balance date.</summary>
    public DateOnly DataSaldoIniziale { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Gets or sets the free-form notes.</summary>
    public string? Note { get; set; }
}

/// <summary>Validator for <see cref="ContoFinanziarioInput"/>.</summary>
public sealed class ContoFinanziarioInputValidator : AbstractValidator<ContoFinanziarioInput>
{
    /// <summary>Initializes a new instance of the <see cref="ContoFinanziarioInputValidator"/> class.</summary>
    public ContoFinanziarioInputValidator()
    {
        RuleFor(x => x.Codice).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Istituto).MaximumLength(200);
        RuleFor(x => x.Iban).MaximumLength(34);
        RuleFor(x => x.Bic).MaximumLength(11);
        RuleFor(x => x.Intestatario).MaximumLength(200);
        RuleFor(x => x.Ultime4Cifre)
            .Must(v => string.IsNullOrEmpty(v) || (v.Length == 4 && v.All(char.IsDigit)))
            .WithMessage("Inserire esattamente 4 cifre.");
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

/// <summary>List query.</summary>
/// <param name="IncludiNonAttivi">Include inactive accounts.</param>
public sealed record ListContiFinanziari(bool IncludiNonAttivi = false)
    : IRequest<IReadOnlyList<ContoFinanziarioListItemDto>>;

/// <summary>Get by id.</summary>
/// <param name="Id">Identifier.</param>
public sealed record GetContoFinanziario(Guid Id) : IRequest<ContoFinanziarioDto?>;

/// <summary>Create command.</summary>
/// <param name="Input">Payload.</param>
public sealed record CreateContoFinanziario(ContoFinanziarioInput Input) : IRequest<Guid>;

/// <summary>Update command.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Input">Payload.</param>
public sealed record UpdateContoFinanziario(Guid Id, ContoFinanziarioInput Input) : IRequest;

/// <summary>Toggle active.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Attivo">Desired state.</param>
public sealed record ToggleContoFinanziarioActivation(Guid Id, bool Attivo) : IRequest;

/// <summary>Handler for <see cref="ListContiFinanziari"/>.</summary>
public sealed class ListContiFinanziariHandler : IRequestHandler<ListContiFinanziari, IReadOnlyList<ContoFinanziarioListItemDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListContiFinanziariHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ListContiFinanziariHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContoFinanziarioListItemDto>> Handle(
        ListContiFinanziari request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.ContiFinanziari.AsNoTracking().AsQueryable();
        if (!request.IncludiNonAttivi)
        {
            query = query.Where(c => c.Attivo);
        }

        var rows = await query
            .OrderBy(c => c.Tipo)
            .ThenBy(c => c.Codice)
            .ToListAsync(cancellationToken);

        // TODO(modulo 07): when MovimentoPrimaNota exists, compute
        // SaldoCorrente = SaldoIniziale + sum(movimenti.Importo)
        // after DataSaldoIniziale. For now return the opening balance.
        return rows
            .Select(c => new ContoFinanziarioListItemDto(
                c.Id,
                c.Codice,
                c.Nome,
                c.Tipo,
                c.Istituto,
                c.Iban,
                c.Ultime4Cifre,
                c.SaldoIniziale,
                c.Valuta,
                c.Attivo))
            .ToList();
    }
}

/// <summary>Handler for <see cref="GetContoFinanziario"/>.</summary>
public sealed class GetContoFinanziarioHandler : IRequestHandler<GetContoFinanziario, ContoFinanziarioDto?>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetContoFinanziarioHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetContoFinanziarioHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<ContoFinanziarioDto?> Handle(GetContoFinanziario request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.ContiFinanziari.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return new ContoFinanziarioDto(
            entity.Id,
            entity.Codice,
            entity.Nome,
            entity.Tipo,
            entity.Istituto,
            entity.Iban,
            entity.Bic,
            entity.Intestatario,
            entity.Ultime4Cifre,
            entity.SaldoIniziale,
            entity.DataSaldoIniziale,
            entity.SaldoIniziale,
            entity.Valuta,
            entity.Attivo,
            entity.Note);
    }
}

/// <summary>Handler for <see cref="CreateContoFinanziario"/>.</summary>
public sealed class CreateContoFinanziarioHandler : IRequestHandler<CreateContoFinanziario, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="CreateContoFinanziarioHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public CreateContoFinanziarioHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateContoFinanziario request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = new ContoFinanziario(
            request.Input.Codice,
            request.Input.Nome,
            request.Input.Tipo,
            request.Input.SaldoIniziale,
            request.Input.DataSaldoIniziale);

        Apply(entity, request.Input);

        db.ContiFinanziari.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    internal static void Apply(ContoFinanziario target, ContoFinanziarioInput input)
    {
        target.Update(
            input.Codice,
            input.Nome,
            input.Tipo,
            input.SaldoIniziale,
            input.DataSaldoIniziale,
            input.Note);

        switch (input.Tipo)
        {
            case TipoConto.Banca:
                target.SetDatiBancari(input.Istituto, input.Iban, input.Bic);
                break;
            case TipoConto.CartaDiCredito:
            case TipoConto.CartaDebitoPrepagata:
                target.SetDatiCarta(input.Istituto, input.Intestatario, input.Ultime4Cifre);
                break;
            default:
                // Cassa: niente dati aggiuntivi.
                break;
        }
    }
}

/// <summary>Validator for <see cref="CreateContoFinanziario"/>.</summary>
public sealed class CreateContoFinanziarioValidator : AbstractValidator<CreateContoFinanziario>
{
    /// <summary>Initializes a new instance of the <see cref="CreateContoFinanziarioValidator"/> class.</summary>
    public CreateContoFinanziarioValidator()
    {
        RuleFor(x => x.Input).NotNull().SetValidator(new ContoFinanziarioInputValidator());
    }
}

/// <summary>Handler for <see cref="UpdateContoFinanziario"/>.</summary>
public sealed class UpdateContoFinanziarioHandler : IRequestHandler<UpdateContoFinanziario>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateContoFinanziarioHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public UpdateContoFinanziarioHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateContoFinanziario request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.ContiFinanziari.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conto finanziario {request.Id} non trovato.");

        CreateContoFinanziarioHandler.Apply(entity, request.Input);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Validator for <see cref="UpdateContoFinanziario"/>.</summary>
public sealed class UpdateContoFinanziarioValidator : AbstractValidator<UpdateContoFinanziario>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateContoFinanziarioValidator"/> class.</summary>
    public UpdateContoFinanziarioValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Input).NotNull().SetValidator(new ContoFinanziarioInputValidator());
    }
}

/// <summary>Handler for <see cref="ToggleContoFinanziarioActivation"/>.</summary>
public sealed class ToggleContoFinanziarioActivationHandler : IRequestHandler<ToggleContoFinanziarioActivation>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ToggleContoFinanziarioActivationHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ToggleContoFinanziarioActivationHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(ToggleContoFinanziarioActivation request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.ContiFinanziari.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Conto finanziario {request.Id} non trovato.");
        entity.SetAttivo(request.Attivo);
        await db.SaveChangesAsync(cancellationToken);
    }
}
