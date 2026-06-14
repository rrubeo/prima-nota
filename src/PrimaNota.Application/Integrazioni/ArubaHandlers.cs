using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.PrimaNota.Import;
using PrimaNota.Domain.Integrazioni;

namespace PrimaNota.Application.Integrazioni;

/// <summary>Aruba integration settings exposed to the admin UI (never returns the password).</summary>
/// <param name="Abilitata">Whether the integration is enabled.</param>
/// <param name="Username">Aruba API username.</param>
/// <param name="UsaDemo">Whether the demo environment is targeted.</param>
/// <param name="HasPassword">Whether a password is already stored.</param>
public sealed record IntegrazioneArubaDto(bool Abilitata, string? Username, bool UsaDemo, bool HasPassword);

/// <summary>Reads the Aruba integration settings.</summary>
public sealed record GetIntegrazioneAruba : IRequest<IntegrazioneArubaDto>;

/// <summary>Updates the Aruba integration settings.</summary>
/// <param name="Abilitata">Whether the integration is enabled.</param>
/// <param name="Username">Aruba API username.</param>
/// <param name="Password">New plaintext password, or null/empty to keep the current one.</param>
/// <param name="UsaDemo">Whether to use the demo environment.</param>
public sealed record UpdateIntegrazioneAruba(bool Abilitata, string? Username, string? Password, bool UsaDemo) : IRequest;

/// <summary>A remote invoice listed by the provider, with the local "already imported" flag.</summary>
/// <param name="Id">Provider id used to download.</param>
/// <param name="IdentificativoSdi">SdI identifier (dedup key).</param>
/// <param name="Numero">Invoice number.</param>
/// <param name="Data">Invoice date.</param>
/// <param name="Controparte">Counterparty name.</param>
/// <param name="ContropartePartitaIva">Counterparty VAT code.</param>
/// <param name="GiaImportata">True if a movement already references this invoice.</param>
public sealed record FatturaRemotaDto(
    string Id,
    string IdentificativoSdi,
    string? Numero,
    DateOnly Data,
    string? Controparte,
    string? ContropartePartitaIva,
    bool GiaImportata);

/// <summary>Lists remote invoices for a direction and date range.</summary>
/// <param name="Direzione">Attiva (sent) or Passiva (received).</param>
/// <param name="Da">Range start.</param>
/// <param name="A">Range end.</param>
public sealed record ListFattureRemote(DirezioneFattura Direzione, DateOnly Da, DateOnly A)
    : IRequest<IReadOnlyList<FatturaRemotaDto>>;

/// <summary>Outcome of a bulk import from the provider.</summary>
/// <param name="Importate">Number of invoices imported.</param>
/// <param name="Saltate">Number skipped because already imported.</param>
/// <param name="Errori">Per-invoice error messages.</param>
public sealed record ImportFattureRemoteResult(int Importate, int Saltate, IReadOnlyList<string> Errori);

/// <summary>Downloads and imports the selected remote invoices.</summary>
/// <param name="Direzione">Attiva (sent) or Passiva (received).</param>
/// <param name="ContoFinanziarioId">Financial account for the created movements.</param>
/// <param name="EsercizioAnno">Target fiscal year.</param>
/// <param name="ArubaIds">Provider ids to import.</param>
public sealed record ImportFattureRemote(
    DirezioneFattura Direzione,
    Guid ContoFinanziarioId,
    int EsercizioAnno,
    IReadOnlyList<string> ArubaIds) : IRequest<ImportFattureRemoteResult>;

/// <summary>Handler for <see cref="GetIntegrazioneAruba"/>.</summary>
public sealed class GetIntegrazioneArubaHandler : IRequestHandler<GetIntegrazioneAruba, IntegrazioneArubaDto>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetIntegrazioneArubaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetIntegrazioneArubaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IntegrazioneArubaDto> Handle(GetIntegrazioneAruba request, CancellationToken cancellationToken)
    {
        var cfg = await db.IntegrazioniAruba.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return cfg is null
            ? new IntegrazioneArubaDto(false, null, false, false)
            : new IntegrazioneArubaDto(cfg.Abilitata, cfg.Username, cfg.UsaDemo, !string.IsNullOrEmpty(cfg.PasswordProtetta));
    }
}

/// <summary>Handler for <see cref="UpdateIntegrazioneAruba"/>.</summary>
public sealed class UpdateIntegrazioneArubaHandler : IRequestHandler<UpdateIntegrazioneAruba>
{
    private readonly IApplicationDbContext db;
    private readonly ISecretProtector protector;

    /// <summary>Initializes a new instance of the <see cref="UpdateIntegrazioneArubaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    /// <param name="protector">Secret protector.</param>
    public UpdateIntegrazioneArubaHandler(IApplicationDbContext db, ISecretProtector protector)
    {
        this.db = db;
        this.protector = protector;
    }

    /// <inheritdoc />
    public async Task Handle(UpdateIntegrazioneAruba request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cfg = await db.IntegrazioniAruba.FirstOrDefaultAsync(cancellationToken);
        if (cfg is null)
        {
            cfg = new IntegrazioneAruba();
            db.IntegrazioniAruba.Add(cfg);
        }

        var protectedPassword = string.IsNullOrWhiteSpace(request.Password)
            ? null
            : protector.Protect(request.Password);

        cfg.Configura(request.Abilitata, request.Username, protectedPassword, request.UsaDemo);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Handler for <see cref="ListFattureRemote"/>.</summary>
public sealed class ListFattureRemoteHandler : IRequestHandler<ListFattureRemote, IReadOnlyList<FatturaRemotaDto>>
{
    private readonly IFatturaProvider provider;
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListFattureRemoteHandler"/> class.</summary>
    /// <param name="provider">Invoice provider.</param>
    /// <param name="db">DB.</param>
    public ListFattureRemoteHandler(IFatturaProvider provider, IApplicationDbContext db)
    {
        this.provider = provider;
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FatturaRemotaDto>> Handle(ListFattureRemote request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remote = await provider.ListAsync(request.Direzione, request.Da, request.A, cancellationToken);
        if (remote.Count == 0)
        {
            return Array.Empty<FatturaRemotaDto>();
        }

        var ids = remote.Select(r => r.IdentificativoSdi).ToList();
        var imported = await db.Movimenti.AsNoTracking()
            .Where(m => m.IdentificativoSdi != null && ids.Contains(m.IdentificativoSdi))
            .Select(m => m.IdentificativoSdi!)
            .ToListAsync(cancellationToken);
        var importedSet = imported.ToHashSet(StringComparer.Ordinal);

        return remote
            .Select(r => new FatturaRemotaDto(
                r.Id,
                r.IdentificativoSdi,
                r.Numero,
                r.Data,
                r.Controparte,
                r.ContropartePartitaIva,
                importedSet.Contains(r.IdentificativoSdi)))
            .ToList();
    }
}

/// <summary>Handler for <see cref="ImportFattureRemote"/>.</summary>
public sealed class ImportFattureRemoteHandler : IRequestHandler<ImportFattureRemote, ImportFattureRemoteResult>
{
    private readonly IFatturaProvider provider;
    private readonly IApplicationDbContext db;
    private readonly IMediator mediator;

    /// <summary>Initializes a new instance of the <see cref="ImportFattureRemoteHandler"/> class.</summary>
    /// <param name="provider">Invoice provider.</param>
    /// <param name="db">DB.</param>
    /// <param name="mediator">Mediator (reuses the single-invoice import handler).</param>
    public ImportFattureRemoteHandler(IFatturaProvider provider, IApplicationDbContext db, IMediator mediator)
    {
        this.provider = provider;
        this.db = db;
        this.mediator = mediator;
    }

    /// <inheritdoc />
    public async Task<ImportFattureRemoteResult> Handle(ImportFattureRemote request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var importate = 0;
        var saltate = 0;
        var errori = new List<string>();

        foreach (var id in request.ArubaIds)
        {
            try
            {
                var scaricata = await provider.DownloadAsync(request.Direzione, id, cancellationToken);

                var exists = await db.Movimenti.AsNoTracking()
                    .AnyAsync(m => m.IdentificativoSdi == scaricata.IdentificativoSdi, cancellationToken);
                if (exists)
                {
                    saltate++;
                    continue;
                }

                using var stream = new MemoryStream(scaricata.Xml);
                await mediator.Send(
                    new ImportFatturaElettronica(
                        stream,
                        request.Direzione,
                        request.ContoFinanziarioId,
                        request.EsercizioAnno,
                        scaricata.IdentificativoSdi),
                    cancellationToken);
                importate++;
            }
            catch (Exception ex)
            {
                errori.Add($"{id}: {ex.Message}");
            }
        }

        return new ImportFattureRemoteResult(importate, saltate, errori);
    }
}
