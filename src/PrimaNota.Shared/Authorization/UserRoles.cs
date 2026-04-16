namespace PrimaNota.Shared.Authorization;

/// <summary>Canonical role names used by Identity and authorization policies.</summary>
public static class UserRoles
{
    /// <summary>System administrator: manages users, roles, configuration.</summary>
    public const string Admin = "Admin";

    /// <summary>Accounting operator: full access to prima nota, anagrafiche, IVA, reporting.</summary>
    public const string Contabile = "Contabile";

    /// <summary>Employee: can only manage own expense notes and attachments.</summary>
    public const string Dipendente = "Dipendente";

    /// <summary>Gets all roles as a read-only collection for seeding.</summary>
    public static IReadOnlyList<string> All { get; } = [Admin, Contabile, Dipendente];
}
