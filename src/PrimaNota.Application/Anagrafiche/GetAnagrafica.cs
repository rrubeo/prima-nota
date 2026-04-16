using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.Anagrafiche.Upsert;

namespace PrimaNota.Application.Anagrafiche;

/// <summary>Retrieves an anagrafica by identifier.</summary>
/// <param name="Id">Identifier of the anagrafica.</param>
public sealed record GetAnagrafica(Guid Id) : IRequest<AnagraficaDto?>;

/// <summary>Handler for <see cref="GetAnagrafica"/>.</summary>
public sealed class GetAnagraficaHandler : IRequestHandler<GetAnagrafica, AnagraficaDto?>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetAnagraficaHandler"/> class.</summary>
    /// <param name="db">Application DB context.</param>
    public GetAnagraficaHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<AnagraficaDto?> Handle(GetAnagrafica request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await db.Anagrafiche
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        return entity?.ToDto();
    }
}
