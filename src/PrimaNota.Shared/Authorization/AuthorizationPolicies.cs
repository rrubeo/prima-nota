namespace PrimaNota.Shared.Authorization;

/// <summary>Canonical names for authorization policies registered in the Web host.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Requires the <see cref="UserRoles.Admin"/> role.</summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>Requires either <see cref="UserRoles.Admin"/> or <see cref="UserRoles.Contabile"/>.</summary>
    public const string RequireContabile = "RequireContabile";

    /// <summary>Requires any authenticated user (Admin, Contabile or Dipendente).</summary>
    public const string RequireAuthenticated = "RequireAuthenticated";
}
