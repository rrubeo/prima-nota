using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.NoteSpese;
using PrimaNota.Domain.PianoConti;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.NoteSpese;

/// <summary>Compact DTO for the list page.</summary>
public sealed record NotaSpeseListItemDto(
    Guid Id,
    string DipendenteNome,
    int Mese,
    int Anno,
    string Descrizione,
    decimal Totale,
    decimal TotaleRimborso,
    int NumeroRighe,
    StatoNotaSpese Stato,
    DateTimeOffset CreatedAt);

/// <summary>Detail DTO.</summary>
public sealed record NotaSpeseDto(
    Guid Id,
    Guid DipendenteId,
    string DipendenteNome,
    int Mese,
    int Anno,
    string Descrizione,
    StatoNotaSpese Stato,
    string? MotivoRifiuto,
    Guid? MovimentoRimborsoId,
    IReadOnlyList<RigaSpesaDto> Righe);

/// <summary>Expense line DTO.</summary>
public sealed record RigaSpesaDto(
    Guid Id,
    DateOnly Data,
    string Descrizione,
    decimal Importo,
    Guid CategoriaId,
    string? CategoriaNome,
    TipoPagamentoSpesa TipoPagamento,
    string? AllegatoPath);

/// <summary>Input for create/update.</summary>
public sealed class NotaSpeseInput
{
    /// <summary>Gets or sets the employee anagrafica id.</summary>
    public Guid DipendenteId { get; set; }

    /// <summary>Gets or sets the reference month.</summary>
    public int Mese { get; set; } = 1;

    /// <summary>Gets or sets the reference year.</summary>
    public int Anno { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Descrizione { get; set; } = string.Empty;

    /// <summary>Gets or sets the expense lines.</summary>
    public List<RigaSpesaInput> Righe { get; set; } = new();
}

/// <summary>Expense line input.</summary>
public sealed class RigaSpesaInput
{
    /// <summary>Gets or sets the expense date.</summary>
    public DateOnly Data { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Descrizione { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public decimal Importo { get; set; }

    /// <summary>Gets or sets the category.</summary>
    public Guid CategoriaId { get; set; }

    /// <summary>Gets or sets how it was paid.</summary>
    public TipoPagamentoSpesa TipoPagamento { get; set; } = TipoPagamentoSpesa.MezzoProprio;
}

/// <summary>Validator for <see cref="NotaSpeseInput"/>.</summary>
public sealed class NotaSpeseInputValidator : AbstractValidator<NotaSpeseInput>
{
    /// <summary>Initializes a new instance of the <see cref="NotaSpeseInputValidator"/> class.</summary>
    public NotaSpeseInputValidator()
    {
        RuleFor(x => x.DipendenteId).NotEmpty();
        RuleFor(x => x.Descrizione).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Mese).InclusiveBetween(1, 12);
        RuleFor(x => x.Anno).GreaterThanOrEqualTo(2000);
        RuleFor(x => x.Righe).NotEmpty();
    }
}

/// <summary>Lists note spese with optional filters.</summary>
/// <param name="Anno">Year filter (null = all).</param>
/// <param name="DipendenteId">Employee filter (null = all).</param>
/// <param name="Stato">State filter (null = all).</param>
public sealed record ListNoteSpese(int? Anno = null, Guid? DipendenteId = null, StatoNotaSpese? Stato = null)
    : IRequest<IReadOnlyList<NotaSpeseListItemDto>>;

/// <summary>Gets a single nota spese.</summary>
/// <param name="Id">Identifier.</param>
public sealed record GetNotaSpese(Guid Id) : IRequest<NotaSpeseDto?>;

/// <summary>Creates a new nota spese in Bozza.</summary>
/// <param name="Input">Payload.</param>
public sealed record CreateNotaSpese(NotaSpeseInput Input) : IRequest<Guid>;

/// <summary>Updates a nota spese (Bozza or Rifiutata).</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Input">Payload.</param>
public sealed record UpdateNotaSpese(Guid Id, NotaSpeseInput Input) : IRequest;

/// <summary>Submits for approval.</summary>
/// <param name="Id">Identifier.</param>
public sealed record InviaNotaSpese(Guid Id) : IRequest;

/// <summary>Approves + auto-generates the reimbursement movement.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="ContoFinanziarioId">Account used for the reimbursement payment.</param>
public sealed record ApprovaNotaSpese(Guid Id, Guid ContoFinanziarioId) : IRequest;

/// <summary>Rejects back to employee.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Motivo">Reason.</param>
public sealed record RifiutaNotaSpese(Guid Id, string Motivo) : IRequest;

/// <summary>Handler for <see cref="ListNoteSpese"/>.</summary>
public sealed class ListNoteSpeseHandler : IRequestHandler<ListNoteSpese, IReadOnlyList<NotaSpeseListItemDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListNoteSpeseHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ListNoteSpeseHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotaSpeseListItemDto>> Handle(ListNoteSpese request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var q = db.NoteSpese.AsNoTracking().Include(n => n.Righe).AsQueryable();

        if (request.Anno is { } anno)
        {
            q = q.Where(n => n.Anno == anno);
        }

        if (request.DipendenteId is { } dipId)
        {
            q = q.Where(n => n.DipendenteId == dipId);
        }

        if (request.Stato is { } stato)
        {
            q = q.Where(n => n.Stato == stato);
        }

        var items = await q.OrderByDescending(n => n.Anno).ThenByDescending(n => n.Mese).ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        var dipIds = items.Select(n => n.DipendenteId).Distinct().ToList();
        var dipNomi = await db.Anagrafiche.AsNoTracking()
            .Where(a => dipIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.RagioneSociale, cancellationToken);

        return items.Select(n => new NotaSpeseListItemDto(
            n.Id,
            dipNomi.GetValueOrDefault(n.DipendenteId, "?"),
            n.Mese,
            n.Anno,
            n.Descrizione,
            n.Totale,
            n.TotaleRimborso,
            n.Righe.Count,
            n.Stato,
            n.CreatedAt)).ToList();
    }
}

/// <summary>Handler for <see cref="GetNotaSpese"/>.</summary>
public sealed class GetNotaSpeseHandler : IRequestHandler<GetNotaSpese, NotaSpeseDto?>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetNotaSpeseHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetNotaSpeseHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<NotaSpeseDto?> Handle(GetNotaSpese request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.NoteSpese.AsNoTracking()
            .Include(n => n.Righe)
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var dipNome = await db.Anagrafiche.AsNoTracking()
            .Where(a => a.Id == entity.DipendenteId)
            .Select(a => a.RagioneSociale)
            .FirstOrDefaultAsync(cancellationToken) ?? "?";

        var catIds = entity.Righe.Select(r => r.CategoriaId).Distinct().ToList();
        var catNomi = await db.Categorie.AsNoTracking()
            .Where(c => catIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nome, cancellationToken);

        return new NotaSpeseDto(
            entity.Id,
            entity.DipendenteId,
            dipNome,
            entity.Mese,
            entity.Anno,
            entity.Descrizione,
            entity.Stato,
            entity.MotivoRifiuto,
            entity.MovimentoRimborsoId,
            entity.Righe.OrderBy(r => r.Data).Select(r => new RigaSpesaDto(
                r.Id,
                r.Data,
                r.Descrizione,
                r.Importo,
                r.CategoriaId,
                catNomi.GetValueOrDefault(r.CategoriaId),
                r.TipoPagamento,
                r.AllegatoPath)).ToList());
    }
}

/// <summary>Handler for <see cref="CreateNotaSpese"/>.</summary>
public sealed class CreateNotaSpeseHandler : IRequestHandler<CreateNotaSpese, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="CreateNotaSpeseHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public CreateNotaSpeseHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateNotaSpese request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var input = request.Input;

        var nota = new Domain.NoteSpese.NotaSpese(input.DipendenteId, input.Mese, input.Anno, input.Descrizione);
        nota.ReplaceRighe(input.Righe.Select(r =>
            new RigaSpesa(r.Data, r.Descrizione, r.Importo, r.CategoriaId, r.TipoPagamento)).ToList());

        db.NoteSpese.Add(nota);
        await db.SaveChangesAsync(cancellationToken);
        return nota.Id;
    }
}

/// <summary>Handler for <see cref="UpdateNotaSpese"/>.</summary>
public sealed class UpdateNotaSpeseHandler : IRequestHandler<UpdateNotaSpese>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateNotaSpeseHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public UpdateNotaSpeseHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateNotaSpese request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.NoteSpese
            .Include(n => n.Righe)
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Nota spese {request.Id} non trovata.");

        var input = request.Input;
        entity.UpdateHeader(input.Descrizione, input.Mese, input.Anno);
        entity.ReplaceRighe(input.Righe.Select(r =>
            new RigaSpesa(r.Data, r.Descrizione, r.Importo, r.CategoriaId, r.TipoPagamento)).ToList());

        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="InviaNotaSpese"/>.</summary>
public sealed class InviaNotaSpeseHandler : IRequestHandler<InviaNotaSpese>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="InviaNotaSpeseHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public InviaNotaSpeseHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(InviaNotaSpese request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.NoteSpese
            .Include(n => n.Righe)
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Nota spese {request.Id} non trovata.");

        entity.Invia();
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="ApprovaNotaSpese"/>.</summary>
public sealed class ApprovaNotaSpeseHandler : IRequestHandler<ApprovaNotaSpese>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ApprovaNotaSpeseHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ApprovaNotaSpeseHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(ApprovaNotaSpese request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.NoteSpese
            .Include(n => n.Righe)
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Nota spese {request.Id} non trovata.");

        entity.Approva();

        // Auto-generate the reimbursement movement if there are lines paid with own money.
        if (entity.TotaleRimborso > 0m)
        {
            var causaleRimborso = await db.Causali.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Codice == "RIMB-SPS", cancellationToken);

            if (causaleRimborso is not null)
            {
                var dipNome = await db.Anagrafiche.AsNoTracking()
                    .Where(a => a.Id == entity.DipendenteId)
                    .Select(a => a.RagioneSociale)
                    .FirstOrDefaultAsync(cancellationToken) ?? "Dipendente";

                var data = DateOnly.FromDateTime(DateTime.UtcNow);
                var desc = $"Rimborso nota spese {entity.Descrizione} - {dipNome}";
                var movimento = new MovimentoPrimaNota(data, entity.Anno, desc, causaleRimborso.Id);
                movimento.UpdateHeader(data, desc, causaleRimborso.Id, null, entity.DipendenteId, null);

                var riga = new RigaMovimento(-entity.TotaleRimborso, request.ContoFinanziarioId, causaleRimborso.CategoriaDefaultId ?? entity.Righe[0].CategoriaId);
                movimento.ReplaceRighe(new[] { riga });

                db.Movimenti.Add(movimento);
                entity.SegnaRimborsata(movimento.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="RifiutaNotaSpese"/>.</summary>
public sealed class RifiutaNotaSpeseHandler : IRequestHandler<RifiutaNotaSpese>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="RifiutaNotaSpeseHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public RifiutaNotaSpeseHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(RifiutaNotaSpese request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.NoteSpese
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Nota spese {request.Id} non trovata.");

        entity.Rifiuta(request.Motivo);
        await db.SaveChangesAsync(cancellationToken);
    }
}
