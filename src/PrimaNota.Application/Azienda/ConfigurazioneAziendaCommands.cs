using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Anagrafiche;
using PrimaNota.Domain.Azienda;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Application.Azienda;

/// <summary>Company configuration DTO returned to the UI.</summary>
/// <param name="Denominazione">Legal / display name.</param>
/// <param name="PartitaIva">VAT number.</param>
/// <param name="CodiceFiscale">Fiscal code.</param>
/// <param name="IndirizzoVia">Street.</param>
/// <param name="IndirizzoCap">Postal code.</param>
/// <param name="IndirizzoCitta">City.</param>
/// <param name="IndirizzoProvincia">Italian province.</param>
/// <param name="IndirizzoCountryCode">ISO country code.</param>
/// <param name="Email">Primary email.</param>
/// <param name="Telefono">Phone.</param>
/// <param name="Pec">PEC email.</param>
/// <param name="EsigibilitaIvaPredefinita">VAT exigibility regime.</param>
public sealed record ConfigurazioneAziendaDto(
    string Denominazione,
    string? PartitaIva,
    string? CodiceFiscale,
    string? IndirizzoVia,
    string? IndirizzoCap,
    string? IndirizzoCitta,
    string? IndirizzoProvincia,
    string IndirizzoCountryCode,
    string? Email,
    string? Telefono,
    string? Pec,
    EsigibilitaIva EsigibilitaIvaPredefinita);

/// <summary>Reads the single company configuration row.</summary>
public sealed record GetConfigurazioneAzienda : IRequest<ConfigurazioneAziendaDto>;

/// <summary>Updates the single company configuration row.</summary>
/// <param name="Denominazione">Legal / display name.</param>
/// <param name="PartitaIva">VAT number.</param>
/// <param name="CodiceFiscale">Fiscal code.</param>
/// <param name="IndirizzoVia">Street.</param>
/// <param name="IndirizzoCap">Postal code.</param>
/// <param name="IndirizzoCitta">City.</param>
/// <param name="IndirizzoProvincia">Italian province (2 chars).</param>
/// <param name="IndirizzoCountryCode">ISO country code (2 chars).</param>
/// <param name="Email">Primary email.</param>
/// <param name="Telefono">Phone.</param>
/// <param name="Pec">PEC email.</param>
/// <param name="EsigibilitaIvaPredefinita">VAT exigibility regime.</param>
public sealed record UpdateConfigurazioneAzienda(
    string Denominazione,
    string? PartitaIva,
    string? CodiceFiscale,
    string? IndirizzoVia,
    string? IndirizzoCap,
    string? IndirizzoCitta,
    string? IndirizzoProvincia,
    string IndirizzoCountryCode,
    string? Email,
    string? Telefono,
    string? Pec,
    EsigibilitaIva EsigibilitaIvaPredefinita) : IRequest;

/// <summary>Validator for <see cref="UpdateConfigurazioneAzienda"/>.</summary>
public sealed class UpdateConfigurazioneAziendaValidator : AbstractValidator<UpdateConfigurazioneAzienda>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateConfigurazioneAziendaValidator"/> class.</summary>
    public UpdateConfigurazioneAziendaValidator()
    {
        RuleFor(x => x.Denominazione).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PartitaIva).MaximumLength(16);
        RuleFor(x => x.CodiceFiscale).MaximumLength(16);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Pec).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Pec));
        RuleFor(x => x.Telefono).MaximumLength(32);
        RuleFor(x => x.IndirizzoVia).MaximumLength(200);
        RuleFor(x => x.IndirizzoCap).MaximumLength(10);
        RuleFor(x => x.IndirizzoCitta).MaximumLength(100);
        RuleFor(x => x.IndirizzoProvincia).MaximumLength(4);
        RuleFor(x => x.IndirizzoCountryCode).NotEmpty().Length(2);
    }
}

/// <summary>Handler for <see cref="GetConfigurazioneAzienda"/>.</summary>
public sealed class GetConfigurazioneAziendaHandler : IRequestHandler<GetConfigurazioneAzienda, ConfigurazioneAziendaDto>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetConfigurazioneAziendaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetConfigurazioneAziendaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<ConfigurazioneAziendaDto> Handle(GetConfigurazioneAzienda request, CancellationToken cancellationToken)
    {
        var c = await db.ConfigurazioneAzienda.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ConfigurazioneAzienda.SingletonId, cancellationToken);

        // Seeder guarantees the row but we fall back to defaults if a fresh DB skipped seeding.
        c ??= new ConfigurazioneAzienda();

        return new ConfigurazioneAziendaDto(
            c.Denominazione,
            c.PartitaIva,
            c.CodiceFiscale,
            c.Indirizzo.Via,
            c.Indirizzo.Cap,
            c.Indirizzo.Citta,
            c.Indirizzo.Provincia,
            c.Indirizzo.CountryCode,
            c.Email,
            c.Telefono,
            c.Pec,
            c.EsigibilitaIvaPredefinita);
    }
}

/// <summary>Handler for <see cref="UpdateConfigurazioneAzienda"/>.</summary>
public sealed class UpdateConfigurazioneAziendaHandler : IRequestHandler<UpdateConfigurazioneAzienda>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateConfigurazioneAziendaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public UpdateConfigurazioneAziendaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(UpdateConfigurazioneAzienda request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await db.ConfigurazioneAzienda
            .FirstOrDefaultAsync(x => x.Id == ConfigurazioneAzienda.SingletonId, cancellationToken);

        if (entity is null)
        {
            entity = new ConfigurazioneAzienda();
            db.ConfigurazioneAzienda.Add(entity);
        }

        entity.UpdateIdentificazione(
            request.Denominazione,
            request.PartitaIva,
            request.CodiceFiscale,
            request.Email,
            request.Telefono,
            request.Pec);

        entity.UpdateIndirizzo(new Indirizzo(
            Normalize(request.IndirizzoVia),
            Normalize(request.IndirizzoCap),
            Normalize(request.IndirizzoCitta),
            Normalize(request.IndirizzoProvincia)?.ToUpperInvariant(),
            request.IndirizzoCountryCode.Trim().ToUpperInvariant()));

        entity.SetEsigibilitaIva(request.EsigibilitaIvaPredefinita);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Normalize(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
