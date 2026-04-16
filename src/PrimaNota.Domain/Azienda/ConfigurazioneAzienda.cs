using PrimaNota.Domain.Abstractions;
using PrimaNota.Domain.Anagrafiche;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Domain.Azienda;

/// <summary>
/// Singleton aggregate that carries company-level parameters the application cannot
/// infer from other data: legal identification, VAT exigibility regime and the
/// information used on reports and official prints. Persisted with a fixed primary
/// key so that the system always has exactly one row.
/// </summary>
public sealed class ConfigurazioneAzienda : AuditableEntity<int>
{
    /// <summary>Fixed primary-key value for the single row.</summary>
    public const int SingletonId = 1;

    /// <summary>Initializes a new instance of the <see cref="ConfigurazioneAzienda"/> class (default, used by the initial seeder).</summary>
    public ConfigurazioneAzienda()
    {
        Id = SingletonId;
        Denominazione = "La mia azienda";
        EsigibilitaIvaPredefinita = EsigibilitaIva.Immediata;
        Indirizzo = Indirizzo.Empty;
    }

    /// <summary>Gets the company legal / display name.</summary>
    public string Denominazione { get; private set; }

    /// <summary>Gets the company VAT number.</summary>
    public string? PartitaIva { get; private set; }

    /// <summary>Gets the company fiscal code.</summary>
    public string? CodiceFiscale { get; private set; }

    /// <summary>Gets the registered office address.</summary>
    public Indirizzo Indirizzo { get; private set; }

    /// <summary>Gets the primary email shown on prints.</summary>
    public string? Email { get; private set; }

    /// <summary>Gets the primary phone shown on prints.</summary>
    public string? Telefono { get; private set; }

    /// <summary>Gets the certified (PEC) email.</summary>
    public string? Pec { get; private set; }

    /// <summary>Gets the company-wide VAT exigibility regime.</summary>
    public EsigibilitaIva EsigibilitaIvaPredefinita { get; private set; }

    /// <summary>Updates identification and contact fields.</summary>
    /// <param name="denominazione">Legal name.</param>
    /// <param name="partitaIva">VAT number.</param>
    /// <param name="codiceFiscale">Fiscal code.</param>
    /// <param name="email">Email.</param>
    /// <param name="telefono">Phone.</param>
    /// <param name="pec">PEC email.</param>
    public void UpdateIdentificazione(
        string denominazione,
        string? partitaIva,
        string? codiceFiscale,
        string? email,
        string? telefono,
        string? pec)
    {
        if (string.IsNullOrWhiteSpace(denominazione))
        {
            throw new ArgumentException("Denominazione obbligatoria.", nameof(denominazione));
        }

        Denominazione = denominazione.Trim();
        PartitaIva = Normalize(partitaIva);
        CodiceFiscale = Normalize(codiceFiscale)?.ToUpperInvariant();
        Email = Normalize(email);
        Telefono = Normalize(telefono);
        Pec = Normalize(pec);
    }

    /// <summary>Replaces the registered office address.</summary>
    /// <param name="indirizzo">New address.</param>
    public void UpdateIndirizzo(Indirizzo indirizzo)
    {
        ArgumentNullException.ThrowIfNull(indirizzo);
        Indirizzo = indirizzo;
    }

    /// <summary>Sets the company-wide VAT exigibility.</summary>
    /// <param name="esigibilita">Desired exigibility.</param>
    public void SetEsigibilitaIva(EsigibilitaIva esigibilita) => EsigibilitaIvaPredefinita = esigibilita;

    private static string? Normalize(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
