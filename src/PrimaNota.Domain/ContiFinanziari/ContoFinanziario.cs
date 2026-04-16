using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>
/// Financial account aggregated by Prima Nota. Holds the static identification of the
/// account (cash register, bank, card, ...) plus the opening balance snapshot used
/// as anchor for the running balance computation. Live balances are computed by
/// summing movements after <see cref="DataSaldoIniziale"/>.
/// </summary>
public sealed class ContoFinanziario : AuditableEntity<Guid>
{
    /// <summary>Initializes a new instance of the <see cref="ContoFinanziario"/> class.</summary>
    /// <param name="codice">Unique short code.</param>
    /// <param name="nome">Display name.</param>
    /// <param name="tipo">Account kind.</param>
    /// <param name="saldoIniziale">Opening balance (signed).</param>
    /// <param name="dataSaldoIniziale">Date at which the opening balance was recorded.</param>
    public ContoFinanziario(
        string codice,
        string nome,
        TipoConto tipo,
        decimal saldoIniziale,
        DateOnly dataSaldoIniziale)
    {
        if (string.IsNullOrWhiteSpace(codice))
        {
            throw new ArgumentException("Codice obbligatorio.", nameof(codice));
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome obbligatorio.", nameof(nome));
        }

        Id = Guid.NewGuid();
        Codice = codice.Trim().ToUpperInvariant();
        Nome = nome.Trim();
        Tipo = tipo;
        SaldoIniziale = saldoIniziale;
        DataSaldoIniziale = dataSaldoIniziale;
        Valuta = "EUR";
        Attivo = true;
    }

    /// <summary>Gets the unique short code (e.g. "CASSA-01", "BPER-IT99", "AMEX-GOLD").</summary>
    public string Codice { get; private set; } = string.Empty;

    /// <summary>Gets the display name.</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>Gets the account kind.</summary>
    public TipoConto Tipo { get; private set; }

    /// <summary>Gets the bank/issuer institution name.</summary>
    public string? Istituto { get; private set; }

    /// <summary>Gets the IBAN for bank accounts.</summary>
    public string? Iban { get; private set; }

    /// <summary>Gets the BIC/SWIFT code for bank accounts.</summary>
    public string? Bic { get; private set; }

    /// <summary>Gets the cardholder name for cards.</summary>
    public string? Intestatario { get; private set; }

    /// <summary>Gets the last four digits of the card number.</summary>
    public string? Ultime4Cifre { get; private set; }

    /// <summary>Gets the opening balance (the anchor for running-balance computation).</summary>
    public decimal SaldoIniziale { get; private set; }

    /// <summary>Gets the date of the opening balance.</summary>
    public DateOnly DataSaldoIniziale { get; private set; }

    /// <summary>Gets the ISO 4217 currency code (always "EUR" in v1).</summary>
    public string Valuta { get; private set; } = "EUR";

    /// <summary>Gets a value indicating whether the account can be used on new movements.</summary>
    public bool Attivo { get; private set; }

    /// <summary>Gets free-form notes.</summary>
    public string? Note { get; private set; }

    /// <summary>Updates common editable fields (identification, opening balance, notes).</summary>
    /// <param name="codice">Code.</param>
    /// <param name="nome">Name.</param>
    /// <param name="tipo">Account kind.</param>
    /// <param name="saldoIniziale">Opening balance.</param>
    /// <param name="dataSaldoIniziale">Opening-balance date.</param>
    /// <param name="note">Notes.</param>
    public void Update(
        string codice,
        string nome,
        TipoConto tipo,
        decimal saldoIniziale,
        DateOnly dataSaldoIniziale,
        string? note)
    {
        if (string.IsNullOrWhiteSpace(codice))
        {
            throw new ArgumentException("Codice obbligatorio.", nameof(codice));
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("Nome obbligatorio.", nameof(nome));
        }

        Codice = codice.Trim().ToUpperInvariant();
        Nome = nome.Trim();
        Tipo = tipo;
        SaldoIniziale = saldoIniziale;
        DataSaldoIniziale = dataSaldoIniziale;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>Sets the bank-specific fields. Applicable when <see cref="Tipo"/> is Banca.</summary>
    /// <param name="istituto">Bank name.</param>
    /// <param name="iban">IBAN.</param>
    /// <param name="bic">BIC/SWIFT.</param>
    public void SetDatiBancari(string? istituto, string? iban, string? bic)
    {
        Istituto = Normalize(istituto);
        Iban = Normalize(iban)?.ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
        Bic = Normalize(bic)?.ToUpperInvariant();
    }

    /// <summary>Sets the card-specific fields.</summary>
    /// <param name="istituto">Card issuer.</param>
    /// <param name="intestatario">Cardholder name.</param>
    /// <param name="ultime4Cifre">Last four digits of the card number.</param>
    public void SetDatiCarta(string? istituto, string? intestatario, string? ultime4Cifre)
    {
        Istituto = Normalize(istituto);
        Intestatario = Normalize(intestatario);

        var normalized = Normalize(ultime4Cifre);
        if (normalized is not null && (normalized.Length != 4 || !normalized.All(char.IsDigit)))
        {
            throw new ArgumentException("Le ultime 4 cifre devono essere 4 numeri.", nameof(ultime4Cifre));
        }

        Ultime4Cifre = normalized;
    }

    /// <summary>Sets the active state.</summary>
    /// <param name="attivo">Desired state.</param>
    public void SetAttivo(bool attivo) => Attivo = attivo;

    private static string? Normalize(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
