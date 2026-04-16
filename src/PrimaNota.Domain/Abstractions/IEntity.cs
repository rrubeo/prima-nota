namespace PrimaNota.Domain.Abstractions;

/// <summary>Marker for domain entities with a strongly-typed identifier.</summary>
/// <typeparam name="TId">Type of the entity identifier.</typeparam>
public interface IEntity<out TId>
    where TId : notnull
{
    /// <summary>Gets the entity primary identifier.</summary>
    TId Id { get; }
}
