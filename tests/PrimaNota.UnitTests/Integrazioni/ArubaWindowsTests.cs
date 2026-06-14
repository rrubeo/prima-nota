using PrimaNota.Infrastructure.Integrazioni;

namespace PrimaNota.UnitTests.Integrazioni;

public sealed class ArubaWindowsTests
{
    [Fact]
    public void SingleDay_ProducesOneWindow()
    {
        var windows = ArubaFatturaProvider.BuildWindows(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 10));

        windows.Should().HaveCount(1);
        windows[0].Start.Should().Be("2026-01-10T00:00:00.000Z");
        windows[0].End.Should().Be("2026-01-11T00:00:00.000Z");
    }

    [Fact]
    public void FiveDays_AreSplitIntoThreeTwoDayWindows()
    {
        var windows = ArubaFatturaProvider.BuildWindows(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5));

        // [1,3), [3,5), [5,6)
        windows.Should().HaveCount(3);
        windows[0].Should().Be(("2026-01-01T00:00:00.000Z", "2026-01-03T00:00:00.000Z"));
        windows[1].Should().Be(("2026-01-03T00:00:00.000Z", "2026-01-05T00:00:00.000Z"));
        windows[2].Should().Be(("2026-01-05T00:00:00.000Z", "2026-01-06T00:00:00.000Z"));
    }

    [Fact]
    public void EveryWindow_SpansAtMostTwoDays()
    {
        var windows = ArubaFatturaProvider.BuildWindows(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        foreach (var (start, end) in windows)
        {
            var s = DateOnly.ParseExact(start[..10], "yyyy-MM-dd");
            var e = DateOnly.ParseExact(end[..10], "yyyy-MM-dd");
            (e.DayNumber - s.DayNumber).Should().BeLessThanOrEqualTo(2);
        }
    }

    [Fact]
    public void ReversedRange_ProducesNoWindows()
    {
        var windows = ArubaFatturaProvider.BuildWindows(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 1));
        windows.Should().BeEmpty();
    }
}
