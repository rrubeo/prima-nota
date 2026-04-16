using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Application.Anagrafiche;

/// <summary>
/// Toggles the active flag of an anagrafica. Soft-disable instead of hard delete,
/// to preserve historical references from movements and audit trail.
/// </summary>
/// <param name="Id">Identifier of the anagrafica.</param>
/// <param name="Attivo">Desired active state.</param>
public sealed record ToggleAnagraficaActivation(Guid Id, bool Attivo) : IRequest;

/// <summary>Handler for <see cref="ToggleAnagraficaActivation"/>.</summary>
public sealed class ToggleAnagraficaActivationHandler : IRequestHandler<ToggleAnagraficaActivation>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ToggleAnagraficaActivationHandler"/> class.</summary>
    /// <param name="db">Application DB context.</param>
    public ToggleAnagraficaActivationHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task Handle(ToggleAnagraficaActivation request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var anagrafica = await db.Anagrafiche.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Anagrafica {request.Id} non trovata.");

        if (request.Attivo)
        {
            anagrafica.Attiva();
        }
        else
        {
            anagrafica.Disattiva();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
