using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.PianoConti;

/// <summary>
/// A reusable template ("causale contabile") that classifies a prima-nota movement
/// with a canonical wording, an operation kind and an optional default category.
/// </summary>
public sealed class Causale : AuditableEntity<Guid>
{
    /// <summary>Initializes a new instance of the <see cref="Causale"/> class.</summary>
    /// <param name="codice">Unique short code.</param>
    /// <param name="nome">Display name.</param>
    /// <param name="tipo">Operation kind.</param>
    public Causale(string codice, string nome, TipoMovimento tipo)
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
        Attiva = true;
    }

    /// <summary>Gets the unique short code.</summary>
    public string Codice { get; private set; } = string.Empty;

    /// <summary>Gets the display name.</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>Gets the operation kind.</summary>
    public TipoMovimento Tipo { get; private set; }

    /// <summary>Gets the default Categoria id suggested when applying this causale.</summary>
    public Guid? CategoriaDefaultId { get; private set; }

    /// <summary>Gets a value indicating whether the causale is usable.</summary>
    public bool Attiva { get; private set; }

    /// <summary>Gets free-form notes.</summary>
    public string? Note { get; private set; }

    /// <summary>Updates editable fields.</summary>
    /// <param name="codice">Code.</param>
    /// <param name="nome">Name.</param>
    /// <param name="tipo">Operation kind.</param>
    /// <param name="categoriaDefaultId">Default category id (nullable).</param>
    /// <param name="note">Notes.</param>
    public void Update(string codice, string nome, TipoMovimento tipo, Guid? categoriaDefaultId, string? note)
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
        CategoriaDefaultId = categoriaDefaultId;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>Sets the active flag.</summary>
    /// <param name="attiva">Desired state.</param>
    public void SetAttiva(bool attiva) => Attiva = attiva;
}
