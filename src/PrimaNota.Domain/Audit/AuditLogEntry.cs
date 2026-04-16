namespace PrimaNota.Domain.Audit;

/// <summary>
/// An immutable record of a security- or business-relevant event.
/// Entries are append-only: they are never updated or deleted by the application.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>Initializes a new instance of the <see cref="AuditLogEntry"/> class.</summary>
    /// <param name="occurredAt">UTC timestamp of the event.</param>
    /// <param name="kind">Categorical kind of the event.</param>
    /// <param name="userId">Identity user id that triggered the event (<c>null</c> if anonymous).</param>
    /// <param name="userName">Display name at the time of the event.</param>
    /// <param name="targetType">Logical type of the affected entity (or empty for non-entity events).</param>
    /// <param name="targetId">Identifier of the affected entity (or empty).</param>
    /// <param name="summary">Short human-readable summary.</param>
    /// <param name="payloadJson">Optional JSON payload with additional details.</param>
    /// <param name="correlationId">Optional correlation id to tie multi-step operations.</param>
    /// <param name="ipAddress">Client IP address captured from the request.</param>
    public AuditLogEntry(
        DateTimeOffset occurredAt,
        AuditEventKind kind,
        string? userId,
        string? userName,
        string targetType,
        string targetId,
        string summary,
        string? payloadJson,
        string? correlationId,
        string? ipAddress)
    {
        Id = Guid.NewGuid();
        OccurredAt = occurredAt;
        Kind = kind;
        UserId = userId;
        UserName = userName;
        TargetType = targetType ?? string.Empty;
        TargetId = targetId ?? string.Empty;
        Summary = summary;
        PayloadJson = payloadJson;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
    }

    /// <summary>Gets the unique identifier of the entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Gets the kind of event.</summary>
    public AuditEventKind Kind { get; private set; }

    /// <summary>Gets the Identity user id of the actor, if any.</summary>
    public string? UserId { get; private set; }

    /// <summary>Gets the actor display name captured at event time.</summary>
    public string? UserName { get; private set; }

    /// <summary>Gets the logical type of the entity affected by the event (empty for non-entity events).</summary>
    public string TargetType { get; private set; }

    /// <summary>Gets the identifier of the entity affected by the event (empty for non-entity events).</summary>
    public string TargetId { get; private set; }

    /// <summary>Gets a short, human-readable summary.</summary>
    public string Summary { get; private set; }

    /// <summary>Gets an optional JSON payload with additional details.</summary>
    public string? PayloadJson { get; private set; }

    /// <summary>Gets an optional correlation id to tie related events together.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>Gets the client IP address captured from the originating request.</summary>
    public string? IpAddress { get; private set; }
}
