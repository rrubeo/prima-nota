namespace PrimaNota.Application.Anagrafiche.Upsert;

/// <summary>
/// Payload shared between Create and Update flows. Holds the full editable state of an anagrafica.
/// </summary>
public sealed record AnagraficaInput
{
    /// <summary>Gets or sets the legal/display name.</summary>
    public string RagioneSociale { get; set; } = string.Empty;

    /// <summary>Gets or sets the first name (natural persons).</summary>
    public string? Nome { get; set; }

    /// <summary>Gets or sets the last name (natural persons).</summary>
    public string? Cognome { get; set; }

    /// <summary>Gets or sets the Italian fiscal code.</summary>
    public string? CodiceFiscale { get; set; }

    /// <summary>Gets or sets the Italian VAT number.</summary>
    public string? PartitaIva { get; set; }

    /// <summary>Gets or sets a value indicating whether the entity is a natural person.</summary>
    public bool PersonaFisica { get; set; }

    /// <summary>Gets or sets a value indicating whether the anagrafica plays the customer role.</summary>
    public bool IsCliente { get; set; }

    /// <summary>Gets or sets a value indicating whether the anagrafica plays the supplier role.</summary>
    public bool IsFornitore { get; set; }

    /// <summary>Gets or sets a value indicating whether the anagrafica is an employee.</summary>
    public bool IsDipendente { get; set; }

    /// <summary>Gets or sets the job title.</summary>
    public string? Mansione { get; set; }

    /// <summary>Gets or sets the hire date.</summary>
    public DateOnly? DataAssunzione { get; set; }

    /// <summary>Gets or sets the termination date.</summary>
    public DateOnly? DataCessazione { get; set; }

    /// <summary>Gets or sets the email.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the phone.</summary>
    public string? Telefono { get; set; }

    /// <summary>Gets or sets the PEC.</summary>
    public string? Pec { get; set; }

    /// <summary>Gets or sets the street.</summary>
    public string? IndirizzoVia { get; set; }

    /// <summary>Gets or sets the postal code.</summary>
    public string? IndirizzoCap { get; set; }

    /// <summary>Gets or sets the city.</summary>
    public string? IndirizzoCitta { get; set; }

    /// <summary>Gets or sets the province.</summary>
    public string? IndirizzoProvincia { get; set; }

    /// <summary>Gets or sets the country code (ISO 2-letter).</summary>
    public string IndirizzoCountryCode { get; set; } = "IT";

    /// <summary>Gets or sets the free-form notes.</summary>
    public string? Note { get; set; }
}
