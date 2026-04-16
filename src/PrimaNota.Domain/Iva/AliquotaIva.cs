using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.Iva;

/// <summary>
/// VAT rate entry. Supports ordinary rates (percentage) and special regimes
/// (exempt, non-taxable, out-of-scope, reverse charge) identified by <see cref="Tipo"/>.
/// </summary>
public sealed class AliquotaIva : AuditableEntity<Guid>
{
    /// <summary>Initializes a new instance of the <see cref="AliquotaIva"/> class.</summary>
    /// <param name="codice">Unique short code.</param>
    /// <param name="descrizione">Human-readable description.</param>
    /// <param name="percentuale">Percentage (0-100). Must be 0 for non-ordinary regimes.</param>
    /// <param name="tipo">VAT treatment.</param>
    public AliquotaIva(string codice, string descrizione, decimal percentuale, TipoIva tipo)
    {
        if (string.IsNullOrWhiteSpace(codice))
        {
            throw new ArgumentException("Codice obbligatorio.", nameof(codice));
        }

        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new ArgumentException("Descrizione obbligatoria.", nameof(descrizione));
        }

        if (percentuale is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentuale), "Percentuale fuori range 0-100.");
        }

        if (tipo != TipoIva.Ordinaria && percentuale != 0m)
        {
            throw new ArgumentException("Solo le aliquote ordinarie possono avere una percentuale > 0.", nameof(percentuale));
        }

        Id = Guid.NewGuid();
        Codice = codice.Trim().ToUpperInvariant();
        Descrizione = descrizione.Trim();
        Percentuale = percentuale;
        Tipo = tipo;
        Attiva = true;
    }

    /// <summary>Gets the unique short code (e.g. "I22", "ESE10").</summary>
    public string Codice { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable description.</summary>
    public string Descrizione { get; private set; } = string.Empty;

    /// <summary>Gets the percentage (always 0 for non-ordinary regimes).</summary>
    public decimal Percentuale { get; private set; }

    /// <summary>Gets the non-deductible percentage (for partial deduction cases).</summary>
    public decimal PercentualeIndetraibile { get; private set; }

    /// <summary>Gets the VAT treatment.</summary>
    public TipoIva Tipo { get; private set; }

    /// <summary>Gets the "natura" code (N1..N7) used on e-invoice XML for non-taxable regimes.</summary>
    public string? CodiceNatura { get; private set; }

    /// <summary>Gets a value indicating whether the rate is active.</summary>
    public bool Attiva { get; private set; }

    /// <summary>Updates editable fields.</summary>
    /// <param name="codice">Code.</param>
    /// <param name="descrizione">Description.</param>
    /// <param name="percentuale">Percentage.</param>
    /// <param name="percentualeIndetraibile">Non-deductible percentage (0-100).</param>
    /// <param name="tipo">Treatment.</param>
    /// <param name="codiceNatura">Natura code (e.g. "N4") for non-ordinary regimes.</param>
    public void Update(
        string codice,
        string descrizione,
        decimal percentuale,
        decimal percentualeIndetraibile,
        TipoIva tipo,
        string? codiceNatura)
    {
        if (string.IsNullOrWhiteSpace(codice))
        {
            throw new ArgumentException("Codice obbligatorio.", nameof(codice));
        }

        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new ArgumentException("Descrizione obbligatoria.", nameof(descrizione));
        }

        if (percentuale is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentuale), "Percentuale fuori range 0-100.");
        }

        if (percentualeIndetraibile is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentualeIndetraibile), "Percentuale indetraibile fuori range 0-100.");
        }

        if (tipo != TipoIva.Ordinaria && percentuale != 0m)
        {
            throw new ArgumentException("Solo le aliquote ordinarie possono avere una percentuale > 0.", nameof(percentuale));
        }

        Codice = codice.Trim().ToUpperInvariant();
        Descrizione = descrizione.Trim();
        Percentuale = percentuale;
        PercentualeIndetraibile = percentualeIndetraibile;
        Tipo = tipo;
        CodiceNatura = string.IsNullOrWhiteSpace(codiceNatura) ? null : codiceNatura.Trim().ToUpperInvariant();
    }

    /// <summary>Sets the active flag.</summary>
    /// <param name="attiva">Desired state.</param>
    public void SetAttiva(bool attiva) => Attiva = attiva;
}
