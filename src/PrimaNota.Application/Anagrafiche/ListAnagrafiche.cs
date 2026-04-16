using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.Anagrafiche.Upsert;

namespace PrimaNota.Application.Anagrafiche;

/// <summary>Lists anagrafiche filtered by role, active state and search text.</summary>
/// <param name="Ruolo">Role filter.</param>
/// <param name="IncludiNonAttive">Include inactive anagrafiche.</param>
/// <param name="Cerca">Free-text search (matches ragione sociale, codice fiscale, partita IVA).</param>
public sealed record ListAnagrafiche(
    AnagraficaRuoloFilter Ruolo = AnagraficaRuoloFilter.Tutti,
    bool IncludiNonAttive = false,
    string? Cerca = null) : IRequest<IReadOnlyList<AnagraficaListItemDto>>;

/// <summary>Handler for <see cref="ListAnagrafiche"/>.</summary>
public sealed class ListAnagraficheHandler : IRequestHandler<ListAnagrafiche, IReadOnlyList<AnagraficaListItemDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ListAnagraficheHandler"/> class.</summary>
    /// <param name="db">Application DB context.</param>
    public ListAnagraficheHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AnagraficaListItemDto>> Handle(
        ListAnagrafiche request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Anagrafiche.AsNoTracking();

        query = request.Ruolo switch
        {
            AnagraficaRuoloFilter.Clienti => query.Where(a => a.IsCliente),
            AnagraficaRuoloFilter.Fornitori => query.Where(a => a.IsFornitore),
            AnagraficaRuoloFilter.Dipendenti => query.Where(a => a.IsDipendente),
            _ => query,
        };

        if (!request.IncludiNonAttive)
        {
            query = query.Where(a => a.Attivo);
        }

        if (!string.IsNullOrWhiteSpace(request.Cerca))
        {
            var needle = request.Cerca.Trim();
            query = query.Where(a =>
                EF.Functions.Like(a.RagioneSociale, $"%{needle}%") ||
                (a.CodiceFiscale != null && EF.Functions.Like(a.CodiceFiscale, $"%{needle}%")) ||
                (a.PartitaIva != null && EF.Functions.Like(a.PartitaIva, $"%{needle}%")));
        }

        var entities = await query
            .OrderBy(a => a.RagioneSociale)
            .Take(500)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToListItem()).ToList();
    }
}
