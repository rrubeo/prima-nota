using PrimaNota.Domain.Audit;

namespace PrimaNota.Application.Abstractions;

/// <summary>
/// Application-level contract for writing entries to the audit log.
/// Infrastructure layer provides the persistent implementation; unit tests can substitute a no-op.
/// </summary>
public interface IAuditLogger
{
    /// <summary>Records a new audit event.</summary>
    /// <param name="kind">Kind of event.</param>
    /// <param name="summary">Short human-readable summary.</param>
    /// <param name="targetType">Logical type of the affected entity, or empty.</param>
    /// <param name="targetId">Identifier of the affected entity, or empty.</param>
    /// <param name="payload">Optional object serialized to JSON for richer context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when the entry has been persisted.</returns>
    Task LogAsync(
        AuditEventKind kind,
        string summary,
        string targetType = "",
        string targetId = "",
        object? payload = null,
        CancellationToken cancellationToken = default);
}
