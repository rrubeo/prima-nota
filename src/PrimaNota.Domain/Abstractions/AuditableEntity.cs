namespace PrimaNota.Domain.Abstractions;

/// <summary>
/// Base class for aggregate roots that track audit metadata (who/when created and updated).
/// Audit fields are populated automatically by the SaveChanges interceptor.
/// </summary>
/// <typeparam name="TId">Type of the entity identifier.</typeparam>
public abstract class AuditableEntity<TId> : IEntity<TId>, IAuditable
    where TId : notnull
{
    /// <inheritdoc />
    public TId Id { get; protected set; } = default!;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; internal set; }

    /// <inheritdoc />
    public string? CreatedBy { get; internal set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; internal set; }

    /// <inheritdoc />
    public string? UpdatedBy { get; internal set; }
}
