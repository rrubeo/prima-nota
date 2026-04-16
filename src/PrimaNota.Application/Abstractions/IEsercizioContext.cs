namespace PrimaNota.Application.Abstractions;

/// <summary>
/// Scoped service that exposes the accounting year currently selected by the user.
/// All feature modules filter their queries by <see cref="Anno"/>.
/// </summary>
public interface IEsercizioContext
{
    /// <summary>Gets the currently selected exercise year.</summary>
    int Anno { get; }

    /// <summary>Overrides the current exercise year (invoked by the year switcher in the AppBar).</summary>
    /// <param name="anno">Year to switch to.</param>
    void SwitchTo(int anno);
}
