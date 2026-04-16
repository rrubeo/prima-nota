namespace PrimaNota.Domain.PrimaNota;

/// <summary>
/// Single cash-flow event that settles (fully or partially) a <see cref="MovimentoPrimaNota"/>
/// representing an invoice. Multiple <see cref="PagamentoMovimento"/> instances against the
/// same invoice support partial payments, advances (acconti) and instalments. The sum of the
/// <see cref="Importo"/> values is always positive: the direction (cash-in or cash-out) is
/// derived from the parent movement's causale. Pro-quota VAT under "IVA per cassa" is computed
/// on the ratio <c>Importo / |Movimento.Totale|</c> applied to the invoice VAT total.
/// </summary>
public sealed class PagamentoMovimento
{
    /// <summary>Initializes a new instance of the <see cref="PagamentoMovimento"/> class.</summary>
    /// <param name="data">Value date of the cash flow.</param>
    /// <param name="importo">Positive settlement amount (always &gt; 0).</param>
    /// <param name="contoFinanziarioId">Financial account that received / paid the amount.</param>
    /// <param name="note">Optional free-form note.</param>
    public PagamentoMovimento(DateOnly data, decimal importo, Guid contoFinanziarioId, string? note = null)
    {
        if (importo <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(importo), importo, "L'importo del pagamento deve essere positivo.");
        }

        if (contoFinanziarioId == Guid.Empty)
        {
            throw new ArgumentException("Conto finanziario obbligatorio.", nameof(contoFinanziarioId));
        }

        Id = Guid.NewGuid();
        Data = data;
        Importo = decimal.Round(importo, 2, MidpointRounding.ToEven);
        ContoFinanziarioId = contoFinanziarioId;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>Initializes a new instance of the <see cref="PagamentoMovimento"/> class for EF Core.</summary>
    private PagamentoMovimento()
    {
    }

    /// <summary>Gets the payment identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the parent movement identifier.</summary>
    public Guid MovimentoId { get; internal set; }

    /// <summary>Gets the value date of the cash flow.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Gets the positive settlement amount.</summary>
    public decimal Importo { get; private set; }

    /// <summary>Gets the financial account involved in the cash flow.</summary>
    public Guid ContoFinanziarioId { get; private set; }

    /// <summary>Gets the optional free-form note.</summary>
    public string? Note { get; private set; }
}
