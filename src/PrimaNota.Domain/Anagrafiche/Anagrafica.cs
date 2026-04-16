using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.Anagrafiche;

/// <summary>
/// Unified anagrafica aggregate: represents customers, suppliers and employees in a
/// single table. A single anagrafica can simultaneously play multiple roles (typical
/// case: a partner that is both a customer and a supplier). Role flags are separate
/// so queries can filter by role without touching the others.
/// </summary>
public sealed class Anagrafica : AuditableEntity<Guid>
{
    /// <summary>Initializes a new instance of the <see cref="Anagrafica"/> class.</summary>
    /// <param name="ragioneSociale">Legal name or full display name.</param>
    /// <param name="personaFisica">Whether this is a natural person (vs. legal entity).</param>
    public Anagrafica(string ragioneSociale, bool personaFisica)
    {
        if (string.IsNullOrWhiteSpace(ragioneSociale))
        {
            throw new ArgumentException("Ragione sociale obbligatoria.", nameof(ragioneSociale));
        }

        Id = Guid.NewGuid();
        RagioneSociale = ragioneSociale.Trim();
        PersonaFisica = personaFisica;
        Contatti = Contatti.Empty;
        Indirizzo = Indirizzo.Empty;
        Attivo = true;
    }

    /// <summary>Gets the legal/display name shown in UI and reports.</summary>
    public string RagioneSociale { get; private set; } = string.Empty;

    /// <summary>Gets the first name for natural persons.</summary>
    public string? Nome { get; private set; }

    /// <summary>Gets the last name for natural persons.</summary>
    public string? Cognome { get; private set; }

    /// <summary>Gets the Italian fiscal code.</summary>
    public string? CodiceFiscale { get; private set; }

    /// <summary>Gets the Italian VAT number (partita IVA).</summary>
    public string? PartitaIva { get; private set; }

    /// <summary>Gets a value indicating whether the anagrafica is a natural person.</summary>
    public bool PersonaFisica { get; private set; }

    /// <summary>Gets a value indicating whether the anagrafica is referenced as a customer.</summary>
    public bool IsCliente { get; private set; }

    /// <summary>Gets a value indicating whether the anagrafica is referenced as a supplier.</summary>
    public bool IsFornitore { get; private set; }

    /// <summary>Gets a value indicating whether the anagrafica is referenced as an employee.</summary>
    public bool IsDipendente { get; private set; }

    /// <summary>Gets the job title (employee only).</summary>
    public string? Mansione { get; private set; }

    /// <summary>Gets the hire date (employee only).</summary>
    public DateOnly? DataAssunzione { get; private set; }

    /// <summary>Gets the termination date (employee only).</summary>
    public DateOnly? DataCessazione { get; private set; }

    /// <summary>Gets the contact info (owned value object).</summary>
    public Contatti Contatti { get; private set; } = Contatti.Empty;

    /// <summary>Gets the main address (owned value object).</summary>
    public Indirizzo Indirizzo { get; private set; } = Indirizzo.Empty;

    /// <summary>Gets a value indicating whether the anagrafica is enabled for use in new movements.</summary>
    public bool Attivo { get; private set; }

    /// <summary>Gets free-form notes about the anagrafica.</summary>
    public string? Note { get; private set; }

    /// <summary>Updates the anagrafica identification fields.</summary>
    /// <param name="ragioneSociale">New legal/display name.</param>
    /// <param name="nome">First name (natural persons).</param>
    /// <param name="cognome">Last name (natural persons).</param>
    /// <param name="codiceFiscale">Fiscal code.</param>
    /// <param name="partitaIva">VAT number.</param>
    /// <param name="personaFisica">Whether this is a natural person.</param>
    public void UpdateIdentificazione(
        string ragioneSociale,
        string? nome,
        string? cognome,
        string? codiceFiscale,
        string? partitaIva,
        bool personaFisica)
    {
        if (string.IsNullOrWhiteSpace(ragioneSociale))
        {
            throw new ArgumentException("Ragione sociale obbligatoria.", nameof(ragioneSociale));
        }

        RagioneSociale = ragioneSociale.Trim();
        Nome = Normalize(nome);
        Cognome = Normalize(cognome);
        CodiceFiscale = Normalize(codiceFiscale)?.ToUpperInvariant();
        PartitaIva = Normalize(partitaIva);
        PersonaFisica = personaFisica;
    }

    /// <summary>Sets the role flags for this anagrafica. At least one role must be true.</summary>
    /// <param name="isCliente">Whether the anagrafica plays the customer role.</param>
    /// <param name="isFornitore">Whether the anagrafica plays the supplier role.</param>
    /// <param name="isDipendente">Whether the anagrafica is an employee.</param>
    public void SetRuoli(bool isCliente, bool isFornitore, bool isDipendente)
    {
        if (!isCliente && !isFornitore && !isDipendente)
        {
            throw new InvalidOperationException("L'anagrafica deve avere almeno un ruolo (cliente, fornitore o dipendente).");
        }

        IsCliente = isCliente;
        IsFornitore = isFornitore;
        IsDipendente = isDipendente;

        if (!IsDipendente)
        {
            Mansione = null;
            DataAssunzione = null;
            DataCessazione = null;
        }
    }

    /// <summary>Sets the employee-specific fields. Requires <see cref="IsDipendente"/>.</summary>
    /// <param name="mansione">Job title.</param>
    /// <param name="dataAssunzione">Hire date.</param>
    /// <param name="dataCessazione">Termination date.</param>
    public void SetDatiDipendente(string? mansione, DateOnly? dataAssunzione, DateOnly? dataCessazione)
    {
        if (!IsDipendente)
        {
            throw new InvalidOperationException("L'anagrafica non e un dipendente.");
        }

        if (dataAssunzione is not null && dataCessazione is not null && dataCessazione < dataAssunzione)
        {
            throw new ArgumentException("Data cessazione precedente alla data di assunzione.");
        }

        Mansione = Normalize(mansione);
        DataAssunzione = dataAssunzione;
        DataCessazione = dataCessazione;
    }

    /// <summary>Replaces the contact info (owned value object).</summary>
    /// <param name="contatti">New contact info.</param>
    public void UpdateContatti(Contatti contatti)
    {
        ArgumentNullException.ThrowIfNull(contatti);
        Contatti = contatti;
    }

    /// <summary>Replaces the address (owned value object).</summary>
    /// <param name="indirizzo">New address.</param>
    public void UpdateIndirizzo(Indirizzo indirizzo)
    {
        ArgumentNullException.ThrowIfNull(indirizzo);
        Indirizzo = indirizzo;
    }

    /// <summary>Updates the free-form notes.</summary>
    /// <param name="note">New notes.</param>
    public void UpdateNote(string? note) => Note = Normalize(note);

    /// <summary>Marks the anagrafica as no longer usable in new movements (kept for audit).</summary>
    public void Disattiva() => Attivo = false;

    /// <summary>Re-enables the anagrafica.</summary>
    public void Attiva() => Attivo = true;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
