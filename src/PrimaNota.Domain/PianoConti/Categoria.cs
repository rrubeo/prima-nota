using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.PianoConti;

/// <summary>
/// Flat accounting category (spec decision: no hierarchy in v1). Each movement line
/// references exactly one Categoria; Causale often nominates a default Categoria.
/// </summary>
public sealed class Categoria : AuditableEntity<Guid>
{
    /// <summary>Initializes a new instance of the <see cref="Categoria"/> class.</summary>
    /// <param name="codice">Short unique code (uppercase).</param>
    /// <param name="nome">Display name.</param>
    /// <param name="natura">Revenue or expense nature.</param>
    public Categoria(string codice, string nome, NaturaCategoria natura)
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
        Natura = natura;
        Attiva = true;
    }

    /// <summary>Gets the short unique code.</summary>
    public string Codice { get; private set; } = string.Empty;

    /// <summary>Gets the display name.</summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>Gets the category nature.</summary>
    public NaturaCategoria Natura { get; private set; }

    /// <summary>Gets a value indicating whether the category can be used on new movements.</summary>
    public bool Attiva { get; private set; }

    /// <summary>Gets free-form notes.</summary>
    public string? Note { get; private set; }

    /// <summary>Updates editable fields.</summary>
    /// <param name="codice">New code.</param>
    /// <param name="nome">New name.</param>
    /// <param name="natura">New nature.</param>
    /// <param name="note">Notes.</param>
    public void Update(string codice, string nome, NaturaCategoria natura, string? note)
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
        Natura = natura;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>Sets the active state.</summary>
    /// <param name="attiva">Desired state.</param>
    public void SetAttiva(bool attiva) => Attiva = attiva;
}
