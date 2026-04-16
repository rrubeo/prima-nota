using Microsoft.AspNetCore.Identity;

namespace PrimaNota.Infrastructure.Identity;

/// <summary>Application-specific role entity for ASP.NET Core Identity.</summary>
public sealed class ApplicationRole : IdentityRole
{
    /// <summary>Initializes a new instance of the <see cref="ApplicationRole"/> class with an unset name.</summary>
    public ApplicationRole()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApplicationRole"/> class with the given role name.</summary>
    /// <param name="roleName">Canonical role name (see <see cref="PrimaNota.Shared.Authorization.UserRoles"/>).</param>
    public ApplicationRole(string roleName)
        : base(roleName)
    {
        NormalizedName = roleName.ToUpperInvariant();
    }

    /// <summary>Gets or sets a human-readable description of the role.</summary>
    public string Description { get; set; } = string.Empty;
}
