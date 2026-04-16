namespace PrimaNota.Domain.Anagrafiche;

/// <summary>
/// Postal address value object. Owned by the parent aggregate (EF Core owned type).
/// </summary>
/// <param name="Via">Street address (e.g. "Via Roma 10").</param>
/// <param name="Cap">Italian postal code (5 digits) or equivalent.</param>
/// <param name="Citta">City name.</param>
/// <param name="Provincia">Italian two-letter province code (e.g. "MI") when applicable.</param>
/// <param name="CountryCode">ISO 3166-1 alpha-2 country code (default "IT").</param>
public sealed record Indirizzo(
    string? Via,
    string? Cap,
    string? Citta,
    string? Provincia,
    string CountryCode = "IT")
{
    /// <summary>Gets an empty address placeholder (all fields null except country = IT).</summary>
    public static Indirizzo Empty { get; } = new(null, null, null, null, "IT");
}
