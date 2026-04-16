using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PrimaNota;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Application.PrimaNota;

/// <summary>Uploads a new attachment for a movement.</summary>
/// <param name="MovimentoId">Movement id.</param>
/// <param name="FileName">Original file name.</param>
/// <param name="MimeType">MIME type.</param>
/// <param name="Content">Content stream (caller keeps ownership — handler reads to end).</param>
public sealed record UploadAllegato(Guid MovimentoId, string FileName, string MimeType, Stream Content) : IRequest<Guid>;

/// <summary>Reads attachment metadata + content for download.</summary>
/// <param name="AllegatoId">Attachment id.</param>
public sealed record GetAllegatoContent(Guid AllegatoId) : IRequest<AllegatoDownload?>;

/// <summary>Removes an attachment.</summary>
/// <param name="MovimentoId">Movement id.</param>
/// <param name="AllegatoId">Attachment id.</param>
public sealed record DeleteAllegato(Guid MovimentoId, Guid AllegatoId) : IRequest;

/// <summary>Download payload.</summary>
/// <param name="FileName">Original file name.</param>
/// <param name="MimeType">MIME type.</param>
/// <param name="Content">Opened read stream (caller disposes).</param>
public sealed record AllegatoDownload(string FileName, string MimeType, Stream Content);

/// <summary>Handler for <see cref="UploadAllegato"/>.</summary>
public sealed class UploadAllegatoHandler : IRequestHandler<UploadAllegato, Guid>
{
    private readonly IApplicationDbContext db;
    private readonly IAttachmentStorage storage;
    private readonly ICurrentUserService currentUser;
    private readonly IDateTimeProvider clock;

    /// <summary>Initializes a new instance of the <see cref="UploadAllegatoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    /// <param name="storage">Attachment storage.</param>
    /// <param name="currentUser">Current user.</param>
    /// <param name="clock">Clock.</param>
    public UploadAllegatoHandler(
        IApplicationDbContext db,
        IAttachmentStorage storage,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        this.db = db;
        this.storage = storage;
        this.currentUser = currentUser;
        this.clock = clock;
    }

    /// <inheritdoc />
    public async Task<Guid> Handle(UploadAllegato request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var movimento = await db.Movimenti
            .Include(m => m.Allegati)
            .FirstOrDefaultAsync(m => m.Id == request.MovimentoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.MovimentoId} non trovato.");

        var subfolder = $"movimenti/{movimento.EsercizioAnno}/{movimento.Id:N}";
        var written = await storage.SaveAsync(subfolder, request.FileName, request.Content, cancellationToken);

        var allegato = new Allegato(
            request.FileName,
            request.MimeType,
            written.Size,
            written.HashSha256,
            written.RelativePath,
            clock.UtcNow,
            currentUser.UserId);

        movimento.AddAllegato(allegato);
        await db.SaveChangesAsync(cancellationToken);
        return allegato.Id;
    }
}

/// <summary>Handler for <see cref="GetAllegatoContent"/>.</summary>
public sealed class GetAllegatoContentHandler : IRequestHandler<GetAllegatoContent, AllegatoDownload?>
{
    private readonly IApplicationDbContext db;
    private readonly IAttachmentStorage storage;

    /// <summary>Initializes a new instance of the <see cref="GetAllegatoContentHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    /// <param name="storage">Storage.</param>
    public GetAllegatoContentHandler(IApplicationDbContext db, IAttachmentStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    /// <inheritdoc />
    public async Task<AllegatoDownload?> Handle(GetAllegatoContent request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var movimento = await db.Movimenti
            .AsNoTracking()
            .Include(m => m.Allegati)
            .Where(m => m.Allegati.Any(a => a.Id == request.AllegatoId))
            .FirstOrDefaultAsync(cancellationToken);

        var allegato = movimento?.Allegati.FirstOrDefault(a => a.Id == request.AllegatoId);
        if (allegato is null)
        {
            return null;
        }

        var stream = storage.OpenRead(allegato.PathRelativo);
        return new AllegatoDownload(allegato.NomeFile, allegato.MimeType, stream);
    }
}

/// <summary>Handler for <see cref="DeleteAllegato"/>.</summary>
public sealed class DeleteAllegatoHandler : IRequestHandler<DeleteAllegato>
{
    private readonly IApplicationDbContext db;
    private readonly IAttachmentStorage storage;

    /// <summary>Initializes a new instance of the <see cref="DeleteAllegatoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    /// <param name="storage">Storage.</param>
    public DeleteAllegatoHandler(IApplicationDbContext db, IAttachmentStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    /// <inheritdoc />
    public async Task Handle(DeleteAllegato request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var movimento = await db.Movimenti
            .Include(m => m.Allegati)
            .FirstOrDefaultAsync(m => m.Id == request.MovimentoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.MovimentoId} non trovato.");

        var removed = movimento.RemoveAllegato(request.AllegatoId);
        if (removed is null)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        storage.Delete(removed.PathRelativo);
    }
}
