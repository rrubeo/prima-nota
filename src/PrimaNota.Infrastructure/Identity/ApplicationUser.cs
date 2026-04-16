using Microsoft.AspNetCore.Identity;

namespace PrimaNota.Infrastructure.Identity;

/// <summary>
/// Application-specific user entity for ASP.NET Core Identity.
/// Extends <see cref="IdentityUser"/> with fields needed by the audit trail and UI.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>Gets or sets the user's full display name (used in UI and audit log).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the user is active and allowed to sign in.
    /// Inactive users exist for audit continuity but cannot authenticate.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp of the last successful login.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
