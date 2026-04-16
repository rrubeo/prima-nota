namespace PrimaNota.Domain.Anagrafiche;

/// <summary>
/// Contact info value object. Owned by the parent aggregate (EF Core owned type).
/// </summary>
/// <param name="Email">Primary email address.</param>
/// <param name="Telefono">Primary phone number (any format).</param>
/// <param name="Pec">Italian certified email address.</param>
public sealed record Contatti(
    string? Email,
    string? Telefono,
    string? Pec)
{
    /// <summary>Gets an empty contacts placeholder.</summary>
    public static Contatti Empty { get; } = new(null, null, null);
}
