using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.Iva;

/// <summary>A row of the VAT register for display/export.</summary>
public sealed record RegistroIvaRigaDto(
    Guid MovimentoId,
    Guid RigaId,
    DateOnly Data,
    string? Numero,
    string CausaleCodice,
    string? AnagraficaRagioneSociale,
    string? PartitaIva,
    string? CodiceFiscale,
    string Descrizione,
    string AliquotaCodice,
    decimal AliquotaPercentuale,
    TipoIva TipoIva,
    decimal Lordo,
    decimal Imponibile,
    decimal Imposta);

/// <summary>Returns all eligible lines of a given VAT register within a period.</summary>
/// <param name="Registro">Sales / Purchases / Retail.</param>
/// <param name="Periodo">Period.</param>
public sealed record GetRegistroIva(TipoRegistroIva Registro, IvaPeriodo Periodo)
    : IRequest<IReadOnlyList<RegistroIvaRigaDto>>;

/// <summary>Handler for <see cref="GetRegistroIva"/>.</summary>
public sealed class GetRegistroIvaHandler : IRequestHandler<GetRegistroIva, IReadOnlyList<RegistroIvaRigaDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetRegistroIvaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetRegistroIvaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistroIvaRigaDto>> Handle(GetRegistroIva request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dataFrom = request.Periodo.DataInizio;
        var dataTo = request.Periodo.DataFine;

        // Restrict causali to the operation kinds relevant for this register.
        var causaleTipiAmmessi = request.Registro switch
        {
            TipoRegistroIva.Corrispettivi => new[] { TipoMovimento.Incasso },
            TipoRegistroIva.Vendite => new[] { TipoMovimento.Incasso },
            TipoRegistroIva.Acquisti => new[] { TipoMovimento.Pagamento, TipoMovimento.RimborsoNotaSpese },
            _ => Array.Empty<TipoMovimento>(),
        };

        var natureAmmesse = request.Registro switch
        {
            TipoRegistroIva.Acquisti => new[] { NaturaCategoria.Uscita },
            _ => new[] { NaturaCategoria.Entrata },
        };

        var rawRows = await (
            from m in db.Movimenti.AsNoTracking()
            where m.EsercizioAnno == request.Periodo.Anno
                  && m.Data >= dataFrom && m.Data <= dataTo
                  && (m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled)
            join c in db.Causali.AsNoTracking() on m.CausaleId equals c.Id
            where causaleTipiAmmessi.Contains(c.Tipo)
            from r in m.Righe
            where r.AliquotaIvaId != null
            join cat in db.Categorie.AsNoTracking() on r.CategoriaId equals cat.Id
            where natureAmmesse.Contains(cat.Natura)
            join al in db.AliquoteIva.AsNoTracking() on r.AliquotaIvaId equals al.Id
            let anagId = r.AnagraficaId ?? m.AnagraficaId
            join an in db.Anagrafiche.AsNoTracking() on anagId equals an.Id into anagJoin
            from anag in anagJoin.DefaultIfEmpty()
            orderby m.Data, m.Numero
            select new
            {
                MovimentoId = m.Id,
                RigaId = r.Id,
                m.Data,
                m.Numero,
                CausaleCodice = c.Codice,
                AnagraficaRagioneSociale = anag != null ? anag.RagioneSociale : null,
                PartitaIva = anag != null ? anag.PartitaIva : null,
                CodiceFiscale = anag != null ? anag.CodiceFiscale : null,
                m.Descrizione,
                AliquotaCodice = al.Codice,
                AliquotaPercentuale = al.Percentuale,
                al.Tipo,
                Lordo = r.Importo,
            }).ToListAsync(cancellationToken);

        return rawRows.Select(row =>
        {
            var (imponibile, imposta) = IvaScorporo.Scorpora(Math.Abs(row.Lordo), row.AliquotaPercentuale);
            var sign = Math.Sign(row.Lordo);
            return new RegistroIvaRigaDto(
                row.MovimentoId,
                row.RigaId,
                row.Data,
                row.Numero,
                row.CausaleCodice,
                row.AnagraficaRagioneSociale,
                row.PartitaIva,
                row.CodiceFiscale,
                row.Descrizione,
                row.AliquotaCodice,
                row.AliquotaPercentuale,
                row.Tipo,
                row.Lordo,
                imponibile * sign,
                imposta * sign);
        }).ToList();
    }
}
