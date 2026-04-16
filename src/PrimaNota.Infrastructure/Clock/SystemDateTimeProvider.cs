using PrimaNota.Shared.Clock;

namespace PrimaNota.Infrastructure.Clock;

/// <summary>Default <see cref="IDateTimeProvider"/> backed by the operating system clock.</summary>
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo ItalyTimeZone = ResolveItalyTimeZone();

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly TodayItaly =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ItalyTimeZone).Date);

    private static TimeZoneInfo ResolveItalyTimeZone()
    {
        // Cross-platform: IANA id on Linux/macOS, Windows id on Windows IIS.
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
