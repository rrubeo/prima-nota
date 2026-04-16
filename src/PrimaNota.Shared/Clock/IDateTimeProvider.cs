namespace PrimaNota.Shared.Clock;

/// <summary>
/// Abstracts access to the current moment in time. Enables deterministic testing
/// by allowing a fake clock to be injected.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Gets the current UTC date and time.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Gets the current date in the Italian (Europe/Rome) time zone.</summary>
    DateOnly TodayItaly { get; }
}
