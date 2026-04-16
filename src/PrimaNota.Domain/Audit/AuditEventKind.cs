namespace PrimaNota.Domain.Audit;

/// <summary>Categorical kinds of audit events tracked by the application.</summary>
public enum AuditEventKind
{
    /// <summary>User signed in successfully.</summary>
    LoginSucceeded = 1,

    /// <summary>Sign-in attempt failed (bad credentials, lockout, not allowed).</summary>
    LoginFailed = 2,

    /// <summary>User signed out.</summary>
    Logout = 3,

    /// <summary>External provider (e.g. Google) sign-in succeeded.</summary>
    ExternalLoginSucceeded = 4,

    /// <summary>External provider sign-in failed.</summary>
    ExternalLoginFailed = 5,

    /// <summary>A password was changed.</summary>
    PasswordChanged = 6,

    /// <summary>A domain entity was created.</summary>
    EntityCreated = 10,

    /// <summary>A domain entity was updated.</summary>
    EntityUpdated = 11,

    /// <summary>A domain entity was deleted.</summary>
    EntityDeleted = 12,

    /// <summary>A movement was reconciled.</summary>
    MovementReconciled = 20,

    /// <summary>A reconciliation was reverted.</summary>
    MovementReconciliationReverted = 21,

    /// <summary>An accounting exercise was closed.</summary>
    EsercizioChiuso = 30,

    /// <summary>An accounting exercise was reopened.</summary>
    EsercizioRiaperto = 31,

    /// <summary>A configuration change performed by an administrator.</summary>
    AdminConfigurationChanged = 40,
}
