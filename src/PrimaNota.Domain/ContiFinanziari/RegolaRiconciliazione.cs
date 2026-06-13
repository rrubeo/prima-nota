using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>
/// A memorized reconciliation choice: maps the <see cref="RegolaSignature"/> of a bank-statement
/// row (cause + operation + description fragment), scoped to a financial account, to the
/// classification the user applied when generating a movement from such a row. On the next
/// matching row the stored classification is suggested (pre-filled), not applied silently.
/// </summary>
public sealed class RegolaRiconciliazione : AuditableEntity<Guid>
{
    /// <summary>Initializes a new instance of the <see cref="RegolaRiconciliazione"/> class.</summary>
    /// <param name="contoFinanziarioId">Financial account the rule applies to.</param>
    /// <param name="signature">Normalized signature of the originating bank row.</param>
    /// <param name="causaleId">Causale to suggest.</param>
    /// <param name="categoriaId">Category to suggest.</param>
    /// <param name="anagraficaId">Optional counterparty to suggest.</param>
    /// <param name="aliquotaIvaId">Optional VAT rate to suggest.</param>
    /// <param name="contoDestinazioneId">Optional destination account (giroconto).</param>
    public RegolaRiconciliazione(
        Guid contoFinanziarioId,
        RegolaSignatureKey signature,
        Guid causaleId,
        Guid categoriaId,
        Guid? anagraficaId,
        Guid? aliquotaIvaId,
        Guid? contoDestinazioneId)
    {
        if (contoFinanziarioId == Guid.Empty)
        {
            throw new ArgumentException("Conto finanziario obbligatorio.", nameof(contoFinanziarioId));
        }

        Id = Guid.NewGuid();
        ContoFinanziarioId = contoFinanziarioId;
        CausaleOperazione = signature.CausaleOperazione;
        Operazione = signature.Operazione;
        DescrizioneChiave = signature.DescrizioneChiave;
        UtilizziCount = 0;
        Aggiorna(causaleId, categoriaId, anagraficaId, aliquotaIvaId, contoDestinazioneId);
    }

    private RegolaRiconciliazione()
    {
    }

    /// <summary>Gets the financial account this rule is scoped to.</summary>
    public Guid ContoFinanziarioId { get; private set; }

    /// <summary>Gets the normalized bank cause code component of the signature.</summary>
    public string CausaleOperazione { get; private set; } = string.Empty;

    /// <summary>Gets the normalized operation-name component of the signature.</summary>
    public string Operazione { get; private set; } = string.Empty;

    /// <summary>Gets the stable description-fragment component of the signature.</summary>
    public string DescrizioneChiave { get; private set; } = string.Empty;

    /// <summary>Gets the suggested causale.</summary>
    public Guid CausaleId { get; private set; }

    /// <summary>Gets the suggested category.</summary>
    public Guid CategoriaId { get; private set; }

    /// <summary>Gets the suggested counterparty (optional).</summary>
    public Guid? AnagraficaId { get; private set; }

    /// <summary>Gets the suggested VAT rate (optional).</summary>
    public Guid? AliquotaIvaId { get; private set; }

    /// <summary>Gets the suggested destination account for giroconti (optional).</summary>
    public Guid? ContoDestinazioneId { get; private set; }

    /// <summary>Gets how many times this rule has been reinforced (used to rank suggestions).</summary>
    public int UtilizziCount { get; private set; }

    /// <summary>
    /// Reinforces the rule with the latest classification: the most recent choice wins and the
    /// usage counter is incremented.
    /// </summary>
    /// <param name="causaleId">Causale to suggest.</param>
    /// <param name="categoriaId">Category to suggest.</param>
    /// <param name="anagraficaId">Optional counterparty.</param>
    /// <param name="aliquotaIvaId">Optional VAT rate.</param>
    /// <param name="contoDestinazioneId">Optional destination account.</param>
    public void Aggiorna(
        Guid causaleId,
        Guid categoriaId,
        Guid? anagraficaId,
        Guid? aliquotaIvaId,
        Guid? contoDestinazioneId)
    {
        if (causaleId == Guid.Empty)
        {
            throw new ArgumentException("Causale obbligatoria.", nameof(causaleId));
        }

        if (categoriaId == Guid.Empty)
        {
            throw new ArgumentException("Categoria obbligatoria.", nameof(categoriaId));
        }

        CausaleId = causaleId;
        CategoriaId = categoriaId;
        AnagraficaId = anagraficaId;
        AliquotaIvaId = aliquotaIvaId;
        ContoDestinazioneId = contoDestinazioneId;
        UtilizziCount++;
    }
}
