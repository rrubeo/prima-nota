using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Esercizi;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Application.Esercizi;

/// <summary>VAT configuration DTO.</summary>
public sealed record EsercizioIvaConfigDto(
    int Anno,
    RegimeIva Regime,
    PeriodicitaIva Periodicita,
    decimal? CoefficienteRedditivitaForfettario,
    StatoEsercizio Stato);

/// <summary>Retrieves the VAT configuration of an exercise.</summary>
/// <param name="Anno">Year.</param>
public sealed record GetEsercizioIvaConfig(int Anno) : IRequest<EsercizioIvaConfigDto?>;

/// <summary>Updates the VAT configuration of an exercise.</summary>
/// <param name="Anno">Year.</param>
/// <param name="Regime">Regime.</param>
/// <param name="Periodicita">Periodicity.</param>
/// <param name="CoefficienteRedditivitaForfettario">Coefficient (required for forfettario).</param>
public sealed record UpdateEsercizioIvaConfig(
    int Anno,
    RegimeIva Regime,
    PeriodicitaIva Periodicita,
    decimal? CoefficienteRedditivitaForfettario) : IRequest;

/// <summary>Validator for <see cref="UpdateEsercizioIvaConfig"/>.</summary>
public sealed class UpdateEsercizioIvaConfigValidator : AbstractValidator<UpdateEsercizioIvaConfig>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateEsercizioIvaConfigValidator"/> class.</summary>
    public UpdateEsercizioIvaConfigValidator()
    {
        RuleFor(x => x.Anno).GreaterThanOrEqualTo(2000);
        When(x => x.Regime == RegimeIva.Forfettario, () =>
        {
            RuleFor(x => x.CoefficienteRedditivitaForfettario)
                .NotNull()
                .InclusiveBetween(0m, 100m)
                .WithMessage("Coefficiente di redditivita obbligatorio e compreso tra 0 e 100%.");
        });
    }
}

/// <summary>Handler for <see cref="GetEsercizioIvaConfig"/>.</summary>
public sealed class GetEsercizioIvaConfigHandler : IRequestHandler<GetEsercizioIvaConfig, EsercizioIvaConfigDto?>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetEsercizioIvaConfigHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetEsercizioIvaConfigHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<EsercizioIvaConfigDto?> Handle(GetEsercizioIvaConfig request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var e = await db.Esercizi
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Anno == request.Anno, cancellationToken);
        return e is null
            ? null
            : new EsercizioIvaConfigDto(
                e.Anno,
                e.RegimeIva,
                e.PeriodicitaIva,
                e.CoefficienteRedditivitaForfettario,
                e.Stato);
    }
}

/// <summary>Handler for <see cref="UpdateEsercizioIvaConfig"/>.</summary>
public sealed class UpdateEsercizioIvaConfigHandler : IRequestHandler<UpdateEsercizioIvaConfig>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateEsercizioIvaConfigHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public UpdateEsercizioIvaConfigHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateEsercizioIvaConfig request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.Esercizi
            .FirstOrDefaultAsync(x => x.Anno == request.Anno, cancellationToken)
            ?? throw new KeyNotFoundException($"Esercizio {request.Anno} non trovato.");

        entity.ConfiguraIva(request.Regime, request.Periodicita, request.CoefficienteRedditivitaForfettario);
        await db.SaveChangesAsync(cancellationToken);
    }
}
