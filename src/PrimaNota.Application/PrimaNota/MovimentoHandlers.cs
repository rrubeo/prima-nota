using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.PrimaNota;

/// <summary>Filters for the list query.</summary>
/// <param name="Anno">Fiscal year.</param>
/// <param name="Stato">Optional state filter.</param>
/// <param name="ContoFinanziarioId">Optional account filter.</param>
/// <param name="CategoriaId">Optional category filter.</param>
/// <param name="AnagraficaId">Optional counterparty filter.</param>
/// <param name="CausaleId">Optional causale filter.</param>
/// <param name="DateFrom">Inclusive from date.</param>
/// <param name="DateTo">Inclusive to date.</param>
/// <param name="ImportoMin">Minimum absolute total.</param>
/// <param name="ImportoMax">Maximum absolute total.</param>
/// <param name="Cerca">Free-text search over description and number.</param>
public sealed record ListMovimenti(
    int Anno,
    StatoMovimento? Stato = null,
    Guid? ContoFinanziarioId = null,
    Guid? CategoriaId = null,
    Guid? AnagraficaId = null,
    Guid? CausaleId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    decimal? ImportoMin = null,
    decimal? ImportoMax = null,
    string? Cerca = null) : IRequest<IReadOnlyList<MovimentoListItemDto>>;

/// <summary>Gets a single movement with all its lines and attachments.</summary>
/// <param name="Id">Identifier.</param>
public sealed record GetMovimento(Guid Id) : IRequest<MovimentoDto?>;

/// <summary>Creates a new movement in Draft state.</summary>
/// <param name="Input">Payload.</param>
public sealed record CreateMovimento(MovimentoInput Input) : IRequest<Guid>;

/// <summary>Updates an existing movement. Requires Draft state.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="Input">Payload.</param>
/// <param name="RowVersion">Optimistic concurrency token.</param>
public sealed record UpdateMovimento(Guid Id, MovimentoInput Input, byte[] RowVersion) : IRequest;

/// <summary>Transitions the movement from Draft to Confirmed.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="RowVersion">Concurrency token.</param>
public sealed record ConfirmMovimento(Guid Id, byte[] RowVersion) : IRequest;

/// <summary>Transitions a Confirmed movement back to Draft (undo conferma).</summary>
/// <param name="Id">Identifier.</param>
/// <param name="RowVersion">Concurrency token.</param>
public sealed record UnconfirmMovimento(Guid Id, byte[] RowVersion) : IRequest;

/// <summary>Deletes a Draft movement.</summary>
/// <param name="Id">Identifier.</param>
/// <param name="RowVersion">Concurrency token.</param>
public sealed record DeleteMovimento(Guid Id, byte[] RowVersion) : IRequest;

/// <summary>Handler for <see cref="ListMovimenti"/>.</summary>
public sealed class ListMovimentiHandler : IRequestHandler<ListMovimenti, IReadOnlyList<MovimentoListItemDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListMovimentiHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ListMovimentiHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MovimentoListItemDto>> Handle(ListMovimenti request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var q = db.Movimenti.AsNoTracking().Where(m => m.EsercizioAnno == request.Anno);

        if (request.Stato is { } stato)
        {
            q = q.Where(m => m.Stato == stato);
        }

        if (request.CausaleId is { } causaleId)
        {
            q = q.Where(m => m.CausaleId == causaleId);
        }

        if (request.AnagraficaId is { } anagId)
        {
            q = q.Where(m => m.AnagraficaId == anagId || m.Righe.Any(r => r.AnagraficaId == anagId));
        }

        if (request.ContoFinanziarioId is { } contoId)
        {
            q = q.Where(m => m.Righe.Any(r => r.ContoFinanziarioId == contoId));
        }

        if (request.CategoriaId is { } catId)
        {
            q = q.Where(m => m.Righe.Any(r => r.CategoriaId == catId));
        }

        if (request.DateFrom is { } from)
        {
            q = q.Where(m => m.Data >= from);
        }

        if (request.DateTo is { } to)
        {
            q = q.Where(m => m.Data <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.Cerca))
        {
            var s = request.Cerca.Trim();
            q = q.Where(m =>
                EF.Functions.Like(m.Descrizione, $"%{s}%") ||
                (m.Numero != null && EF.Functions.Like(m.Numero, $"%{s}%")));
        }

        var projected = await (
            from m in q.OrderByDescending(x => x.Data).ThenByDescending(x => x.CreatedAt)
            join c in db.Causali.AsNoTracking() on m.CausaleId equals c.Id
            join a in db.Anagrafiche.AsNoTracking() on m.AnagraficaId equals a.Id into anagJoin
            from anag in anagJoin.DefaultIfEmpty()
            select new
            {
                m.Id,
                m.Data,
                m.Descrizione,
                m.Numero,
                CausaleCodice = c.Codice,
                CausaleNome = c.Nome,
                AnagraficaRagioneSociale = anag != null ? anag.RagioneSociale : null,
                Totale = m.Righe.Sum(r => r.Importo),
                NumeroRighe = m.Righe.Count,
                m.Stato,
                AllegatiCount = m.Allegati.Count,
            })
            .Take(1000)
            .ToListAsync(cancellationToken);

        var filtered = projected.AsEnumerable();
        if (request.ImportoMin is { } min)
        {
            filtered = filtered.Where(p => Math.Abs(p.Totale) >= min);
        }

        if (request.ImportoMax is { } max)
        {
            filtered = filtered.Where(p => Math.Abs(p.Totale) <= max);
        }

        return filtered
            .Select(p => new MovimentoListItemDto(
                p.Id,
                p.Data,
                p.Descrizione,
                p.Numero,
                p.CausaleCodice,
                p.CausaleNome,
                p.AnagraficaRagioneSociale,
                p.Totale,
                p.NumeroRighe,
                p.Stato,
                p.AllegatiCount))
            .ToList();
    }
}

/// <summary>Handler for <see cref="GetMovimento"/>.</summary>
public sealed class GetMovimentoHandler : IRequestHandler<GetMovimento, MovimentoDto?>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetMovimentoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<MovimentoDto?> Handle(GetMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Movimenti
            .AsNoTracking()
            .Include(m => m.Righe)
            .Include(m => m.Allegati)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new MovimentoDto(
            entity.Id,
            entity.Data,
            entity.EsercizioAnno,
            entity.Descrizione,
            entity.Numero,
            entity.CausaleId,
            entity.AnagraficaId,
            entity.Stato,
            entity.Note,
            entity.RowVersion,
            entity.Righe
                .Select(r => new RigaMovimentoDto(
                    r.Id,
                    r.Importo,
                    r.ContoFinanziarioId,
                    r.CategoriaId,
                    r.AnagraficaId,
                    r.AliquotaIvaId,
                    r.Note))
                .ToList(),
            entity.Allegati
                .Select(a => new AllegatoDto(a.Id, a.NomeFile, a.MimeType, a.Size, a.UploadedAt))
                .ToList());
    }
}

/// <summary>Handler for <see cref="CreateMovimento"/>.</summary>
public sealed class CreateMovimentoHandler : IRequestHandler<CreateMovimento, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="CreateMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public CreateMovimentoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var input = request.Input;

        var movimento = new MovimentoPrimaNota(input.Data, input.EsercizioAnno, input.Descrizione, input.CausaleId);
        movimento.UpdateHeader(input.Data, input.Descrizione, input.CausaleId, input.Numero, input.AnagraficaId, input.Note);

        var righe = input.Righe.Select(BuildRiga).ToList();
        movimento.ReplaceRighe(righe);

        db.Movimenti.Add(movimento);
        await db.SaveChangesAsync(cancellationToken);
        return movimento.Id;
    }

    internal static RigaMovimento BuildRiga(RigaMovimentoInput r)
    {
        var riga = new RigaMovimento(r.Importo, r.ContoFinanziarioId, r.CategoriaId);
        riga.SetAnagrafica(r.AnagraficaId);
        riga.SetAliquotaIva(r.AliquotaIvaId);
        riga.SetNote(r.Note);
        return riga;
    }
}

/// <summary>Validator for <see cref="CreateMovimento"/>.</summary>
public sealed class CreateMovimentoValidator : AbstractValidator<CreateMovimento>
{
    /// <summary>Initializes a new instance of the <see cref="CreateMovimentoValidator"/> class.</summary>
    public CreateMovimentoValidator()
    {
        RuleFor(x => x.Input).NotNull().SetValidator(new MovimentoInputValidator());
    }
}

/// <summary>Handler for <see cref="UpdateMovimento"/>.</summary>
public sealed class UpdateMovimentoHandler : IRequestHandler<UpdateMovimento>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public UpdateMovimentoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.Movimenti
            .Include(m => m.Righe)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.Id} non trovato.");

        ((Microsoft.EntityFrameworkCore.DbContext)db).Entry(entity).Property(e => e.RowVersion).OriginalValue = request.RowVersion;

        var input = request.Input;
        entity.UpdateHeader(input.Data, input.Descrizione, input.CausaleId, input.Numero, input.AnagraficaId, input.Note);
        entity.ReplaceRighe(input.Righe.Select(CreateMovimentoHandler.BuildRiga).ToList());

        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Validator for <see cref="UpdateMovimento"/>.</summary>
public sealed class UpdateMovimentoValidator : AbstractValidator<UpdateMovimento>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateMovimentoValidator"/> class.</summary>
    public UpdateMovimentoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Input).NotNull().SetValidator(new MovimentoInputValidator());
    }
}

/// <summary>Handler for <see cref="ConfirmMovimento"/>.</summary>
public sealed class ConfirmMovimentoHandler : IRequestHandler<ConfirmMovimento>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ConfirmMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ConfirmMovimentoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(ConfirmMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Movimenti
            .Include(m => m.Righe)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.Id} non trovato.");

        ((Microsoft.EntityFrameworkCore.DbContext)db).Entry(entity).Property(e => e.RowVersion).OriginalValue = request.RowVersion;

        entity.Confirm();
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="UnconfirmMovimento"/>.</summary>
public sealed class UnconfirmMovimentoHandler : IRequestHandler<UnconfirmMovimento>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UnconfirmMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public UnconfirmMovimentoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UnconfirmMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Movimenti
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.Id} non trovato.");

        ((Microsoft.EntityFrameworkCore.DbContext)db).Entry(entity).Property(e => e.RowVersion).OriginalValue = request.RowVersion;

        entity.Unconfirm();
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="DeleteMovimento"/>.</summary>
public sealed class DeleteMovimentoHandler : IRequestHandler<DeleteMovimento>
{
    private readonly IApplicationDbContext db;
    private readonly IAttachmentStorage storage;

    /// <summary>Initializes a new instance of the <see cref="DeleteMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    /// <param name="storage">Attachment storage.</param>
    public DeleteMovimentoHandler(IApplicationDbContext db, IAttachmentStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    /// <inheritdoc />
    public async Task Handle(DeleteMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Movimenti
            .Include(m => m.Allegati)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.Id} non trovato.");

        if (entity.Stato != StatoMovimento.Draft)
        {
            throw new InvalidOperationException(
                $"Eliminazione non consentita: il movimento e in stato {entity.Stato}. Riportalo in Draft prima.");
        }

        ((Microsoft.EntityFrameworkCore.DbContext)db).Entry(entity).Property(e => e.RowVersion).OriginalValue = request.RowVersion;

        var pathsToDelete = entity.Allegati.Select(a => a.PathRelativo).ToList();

        db.Movimenti.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var path in pathsToDelete)
        {
            storage.Delete(path);
        }
    }
}
