using PrimaNota.Domain.Anagrafiche;

namespace PrimaNota.Application.Anagrafiche.Upsert;

/// <summary>
/// Static mapping helpers between <see cref="Anagrafica"/>, <see cref="AnagraficaDto"/>
/// and <see cref="AnagraficaInput"/>. Avoids Mapster reflection for this small surface.
/// </summary>
public static class AnagraficaMappings
{
    /// <summary>Maps a domain entity to the detail DTO.</summary>
    /// <param name="source">Entity.</param>
    /// <returns>DTO.</returns>
    public static AnagraficaDto ToDto(this Anagrafica source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new AnagraficaDto(
            source.Id,
            source.RagioneSociale,
            source.Nome,
            source.Cognome,
            source.CodiceFiscale,
            source.PartitaIva,
            source.PersonaFisica,
            source.IsCliente,
            source.IsFornitore,
            source.IsDipendente,
            source.Mansione,
            source.DataAssunzione,
            source.DataCessazione,
            source.Contatti.Email,
            source.Contatti.Telefono,
            source.Contatti.Pec,
            source.Indirizzo.Via,
            source.Indirizzo.Cap,
            source.Indirizzo.Citta,
            source.Indirizzo.Provincia,
            source.Indirizzo.CountryCode,
            source.Attivo,
            source.Note,
            source.CreatedAt,
            source.UpdatedAt);
    }

    /// <summary>Maps a domain entity to the list projection.</summary>
    /// <param name="source">Entity.</param>
    /// <returns>List DTO.</returns>
    public static AnagraficaListItemDto ToListItem(this Anagrafica source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new AnagraficaListItemDto(
            source.Id,
            source.RagioneSociale,
            source.CodiceFiscale,
            source.PartitaIva,
            source.IsCliente,
            source.IsFornitore,
            source.IsDipendente,
            source.Contatti.Email,
            source.Contatti.Telefono,
            source.Attivo);
    }

    /// <summary>Applies values from <see cref="AnagraficaInput"/> to a domain entity.</summary>
    /// <param name="target">Entity to update in place.</param>
    /// <param name="input">Input payload.</param>
    public static void ApplyInput(this Anagrafica target, AnagraficaInput input)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(input);

        target.UpdateIdentificazione(
            input.RagioneSociale,
            input.Nome,
            input.Cognome,
            input.CodiceFiscale,
            input.PartitaIva,
            input.PersonaFisica);

        target.SetRuoli(input.IsCliente, input.IsFornitore, input.IsDipendente);

        if (input.IsDipendente)
        {
            target.SetDatiDipendente(input.Mansione, input.DataAssunzione, input.DataCessazione);
        }

        target.UpdateContatti(new Contatti(
            string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
            string.IsNullOrWhiteSpace(input.Telefono) ? null : input.Telefono.Trim(),
            string.IsNullOrWhiteSpace(input.Pec) ? null : input.Pec.Trim()));

        target.UpdateIndirizzo(new Indirizzo(
            string.IsNullOrWhiteSpace(input.IndirizzoVia) ? null : input.IndirizzoVia.Trim(),
            string.IsNullOrWhiteSpace(input.IndirizzoCap) ? null : input.IndirizzoCap.Trim(),
            string.IsNullOrWhiteSpace(input.IndirizzoCitta) ? null : input.IndirizzoCitta.Trim(),
            string.IsNullOrWhiteSpace(input.IndirizzoProvincia) ? null : input.IndirizzoProvincia.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(input.IndirizzoCountryCode) ? "IT" : input.IndirizzoCountryCode.Trim().ToUpperInvariant()));

        target.UpdateNote(input.Note);
    }
}
