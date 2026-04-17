namespace PrimaNota.Domain.NoteSpese;

/// <summary>
/// Single expense line within a <see cref="NotaSpese"/>. Represents one receipt / invoice
/// uploaded by the employee with its amount, category and payment method.
/// </summary>
public sealed class RigaSpesa
{
    /// <summary>Initializes a new instance of the <see cref="RigaSpesa"/> class.</summary>
    /// <param name="data">Date the expense was incurred.</param>
    /// <param name="descrizione">Short description of the expense.</param>
    /// <param name="importo">Positive amount.</param>
    /// <param name="categoriaId">Expense category.</param>
    /// <param name="tipoPagamento">How it was paid.</param>
    public RigaSpesa(
        DateOnly data,
        string descrizione,
        decimal importo,
        Guid categoriaId,
        TipoPagamentoSpesa tipoPagamento)
    {
        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new ArgumentException("Descrizione obbligatoria.", nameof(descrizione));
        }

        if (importo <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(importo), importo, "L'importo deve essere positivo.");
        }

        if (categoriaId == Guid.Empty)
        {
            throw new ArgumentException("Categoria obbligatoria.", nameof(categoriaId));
        }

        Id = Guid.NewGuid();
        Data = data;
        Descrizione = descrizione.Trim();
        Importo = decimal.Round(importo, 2, MidpointRounding.ToEven);
        CategoriaId = categoriaId;
        TipoPagamento = tipoPagamento;
    }

    private RigaSpesa()
    {
    }

    /// <summary>Gets the line identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the parent nota spese identifier.</summary>
    public Guid NotaSpeseId { get; internal set; }

    /// <summary>Gets the expense date.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Gets the description.</summary>
    public string Descrizione { get; private set; } = string.Empty;

    /// <summary>Gets the positive amount.</summary>
    public decimal Importo { get; private set; }

    /// <summary>Gets the expense category.</summary>
    public Guid CategoriaId { get; private set; }

    /// <summary>Gets how the expense was paid.</summary>
    public TipoPagamentoSpesa TipoPagamento { get; private set; }

    /// <summary>Gets the optional path to the uploaded receipt/scan.</summary>
    public string? AllegatoPath { get; private set; }

    /// <summary>Sets the receipt attachment path.</summary>
    /// <param name="path">Relative path under the attachments root.</param>
    public void SetAllegato(string? path) => AllegatoPath = path?.Replace('\\', '/');
}
