namespace PrimaNota.Domain.Abstractions;

/// <summary>
/// Marker for entities whose creation and last modification metadata
/// must be populated automatically by the persistence layer.
/// </summary>
public interface IAuditable
{
    /// <summary>Gets the UTC timestamp when the entity was first persisted.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the identifier of the user who created the entity.</summary>
    string? CreatedBy { get; }

    /// <summary>Gets the UTC timestamp of the last modification.</summary>
    DateTimeOffset? UpdatedAt { get; }

    /// <summary>Gets the identifier of the user who last modified the entity.</summary>
    string? UpdatedBy { get; }
}
