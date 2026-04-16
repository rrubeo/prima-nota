using System.Text.RegularExpressions;
using FluentValidation;

namespace PrimaNota.Application.Anagrafiche.Upsert;

/// <summary>Validates an <see cref="AnagraficaInput"/> before it is persisted.</summary>
public sealed partial class AnagraficaInputValidator : AbstractValidator<AnagraficaInput>
{
    /// <summary>Initializes a new instance of the <see cref="AnagraficaInputValidator"/> class.</summary>
    public AnagraficaInputValidator()
    {
        RuleFor(x => x.RagioneSociale)
            .NotEmpty().WithMessage("Ragione sociale obbligatoria.")
            .MaximumLength(200);

        RuleFor(x => x.Nome).MaximumLength(100);
        RuleFor(x => x.Cognome).MaximumLength(100);

        RuleFor(x => x.CodiceFiscale)
            .Must(BeValidCodiceFiscale)
            .WithMessage("Codice fiscale in formato non valido.")
            .When(x => !string.IsNullOrWhiteSpace(x.CodiceFiscale));

        RuleFor(x => x.PartitaIva)
            .Must(BeValidPartitaIva)
            .WithMessage("Partita IVA in formato non valido.")
            .When(x => !string.IsNullOrWhiteSpace(x.PartitaIva));

        RuleFor(x => x)
            .Must(x => x.IsCliente || x.IsFornitore || x.IsDipendente)
            .WithMessage("Selezionare almeno un ruolo (cliente, fornitore o dipendente).");

        When(x => x.IsDipendente, () =>
        {
            RuleFor(x => x.Mansione).MaximumLength(100);
            RuleFor(x => x)
                .Must(x => x.DataAssunzione is null || x.DataCessazione is null || x.DataCessazione >= x.DataAssunzione)
                .WithMessage("La data di cessazione non puo precedere la data di assunzione.");
        });

        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Pec).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Pec));
        RuleFor(x => x.Telefono).MaximumLength(32);

        RuleFor(x => x.IndirizzoVia).MaximumLength(200);
        RuleFor(x => x.IndirizzoCap).MaximumLength(10);
        RuleFor(x => x.IndirizzoCitta).MaximumLength(100);
        RuleFor(x => x.IndirizzoProvincia).MaximumLength(4);
        RuleFor(x => x.IndirizzoCountryCode)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Z]{2}$").WithMessage("Codice paese ISO a 2 lettere.");

        RuleFor(x => x.Note).MaximumLength(2000);
    }

    private static bool BeValidCodiceFiscale(string? value) =>
        value is null || CodiceFiscalePattern().IsMatch(value.Trim().ToUpperInvariant());

    private static bool BeValidPartitaIva(string? value) =>
        value is null || PartitaIvaPattern().IsMatch(value.Trim());

    [GeneratedRegex(@"^[A-Z0-9]{11,16}$")]
    private static partial Regex CodiceFiscalePattern();

    [GeneratedRegex(@"^[0-9]{11}$")]
    private static partial Regex PartitaIvaPattern();
}
