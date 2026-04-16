using System.ComponentModel.DataAnnotations;

namespace PrimaNota.Infrastructure.Identity;

/// <summary>
/// First-run bootstrap options. When populated, creates the initial admin user on startup.
/// Must be cleared from production configuration after the first successful login.
/// </summary>
public sealed class IdentityBootstrapOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Identity:Bootstrap";

    /// <summary>Gets or sets the email address of the initial admin user.</summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>Gets or sets the password of the initial admin user (strong password required).</summary>
    [MinLength(12)]
    public string? Password { get; set; }

    /// <summary>Gets or sets the full name of the initial admin user.</summary>
    public string? FullName { get; set; }

    /// <summary>Gets a value indicating whether bootstrap is enabled (all fields provided).</summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(FullName);
}
