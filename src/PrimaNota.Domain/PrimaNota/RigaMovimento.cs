using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.PrimaNota;

/// <summary>
/// A single line inside a <see cref="MovimentoPrimaNota"/>. Each line imputes an amount
/// (signed: positive = inflow, negative = outflow) to exactly one financial account and
/// categorises it. A movement with multiple lines represents either a split (multiple
/// categories on the same account) or an internal transfer (two accounts, sum = 0).
/// </summary>
public sealed class RigaMovimento : IEntity<Guid>
{
    /// <summary>Initializes a new instance of the <see cref="RigaMovimento"/> class.</summary>
    /// <param name="importo">Signed amount (positive = inflow, negative = outflow, non-zero).</param>
    /// <param name="contoFinanziarioId">Financial account that receives the imputation.</param>
    /// <param name="categoriaId">Category that classifies this line.</param>
    public RigaMovimento(decimal importo, Guid contoFinanziarioId, Guid categoriaId)
    {
        if (importo == 0m)
        {
            throw new ArgumentException("L'importo della riga non puo essere zero.", nameof(importo));
        }

        if (contoFinanziarioId == Guid.Empty)
        {
            throw new ArgumentException("Conto finanziario obbligatorio.", nameof(contoFinanziarioId));
        }

        if (categoriaId == Guid.Empty)
        {
            throw new ArgumentException("Categoria obbligatoria.", nameof(categoriaId));
        }

        Id = Guid.NewGuid();
        Importo = decimal.Round(importo, 2, MidpointRounding.ToEven);
        ContoFinanziarioId = contoFinanziarioId;
        CategoriaId = categoriaId;
    }

    /// <summary>Initializes a new instance of the <see cref="RigaMovimento"/> class for EF Core.</summary>
    private RigaMovimento()
    {
    }

    /// <summary>Gets the line identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the parent movement identifier.</summary>
    public Guid MovimentoId { get; internal set; }

    /// <summary>Gets the signed amount in euro, rounded to 2 decimals.</summary>
    public decimal Importo { get; private set; }

    /// <summary>Gets the financial account that this line impacts.</summary>
    public Guid ContoFinanziarioId { get; private set; }

    /// <summary>Gets the category assigned to this line.</summary>
    public Guid CategoriaId { get; private set; }

    /// <summary>Gets the optional counterparty (cliente/fornitore/dipendente) for this line.</summary>
    public Guid? AnagraficaId { get; private set; }

    /// <summary>Gets the optional VAT rate for this line.</summary>
    public Guid? AliquotaIvaId { get; private set; }

    /// <summary>Gets the optional line note.</summary>
    public string? Note { get; private set; }

    /// <summary>Replaces the core imputation fields.</summary>
    /// <param name="importo">New amount.</param>
    /// <param name="contoFinanziarioId">New account.</param>
    /// <param name="categoriaId">New category.</param>
    public void UpdateImputazione(decimal importo, Guid contoFinanziarioId, Guid categoriaId)
    {
        if (importo == 0m)
        {
            throw new ArgumentException("L'importo della riga non puo essere zero.", nameof(importo));
        }

        Importo = decimal.Round(importo, 2, MidpointRounding.ToEven);
        ContoFinanziarioId = contoFinanziarioId;
        CategoriaId = categoriaId;
    }

    /// <summary>Sets the optional anagrafica link.</summary>
    /// <param name="anagraficaId">Anagrafica id or null.</param>
    public void SetAnagrafica(Guid? anagraficaId) => AnagraficaId = anagraficaId;

    /// <summary>Sets the optional VAT rate link.</summary>
    /// <param name="aliquotaIvaId">Rate id or null.</param>
    public void SetAliquotaIva(Guid? aliquotaIvaId) => AliquotaIvaId = aliquotaIvaId;

    /// <summary>Sets the optional line note.</summary>
    /// <param name="note">Free-form note.</param>
    public void SetNote(string? note) =>
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
