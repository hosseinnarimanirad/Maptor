using System.Collections.Generic;
using System.Threading;
using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Tests.Astronomy;

public class MapClockTest
{
    private static readonly DateTime Start = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FakeWallClock
    {
        public DateTime Now { get; set; } = Start;
    }

    [Fact]
    public void Live_FollowsTheWallClockOnEveryTick()
    {
        var wall = new FakeWallClock();

        using var clock = new MapClock(() => wall.Now);

        clock.IsLive = true;

        wall.Now = Start.AddMinutes(5);

        clock.Tick();

        Assert.Equal(Start.AddMinutes(5), clock.Time);
    }

    [Fact]
    public void SettingTheTimeByHand_LeavesLiveMode()
    {
        var wall = new FakeWallClock();

        using var clock = new MapClock(() => wall.Now) { IsLive = true };

        clock.Time = Start.AddDays(-3);

        Assert.False(clock.IsLive);

        wall.Now = Start.AddMinutes(1);

        clock.Tick();

        Assert.Equal(Start.AddDays(-3), clock.Time);
    }

    [Fact]
    public void Playing_AdvancesByIntervalTimesSpeed_AndCanRunBackwards()
    {
        using var clock = new MapClock(() => Start) { TickInterval = TimeSpan.FromMilliseconds(200), Speed = 600 };

        clock.IsPlaying = true;

        clock.Tick();

        Assert.Equal(Start.AddMinutes(2), clock.Time);

        clock.Speed = -600;

        clock.Tick();

        Assert.Equal(Start, clock.Time);
    }

    [Fact]
    public void LiveAndPlaying_ExcludeEachOther()
    {
        using var clock = new MapClock(() => Start);

        clock.IsLive = true;
        clock.IsPlaying = true;

        Assert.False(clock.IsLive);

        clock.IsLive = true;

        Assert.False(clock.IsPlaying);
    }

    [Fact]
    public void Stopped_TickChangesNothing()
    {
        using var clock = new MapClock(() => Start);

        int changes = 0;

        clock.TimeChanged += (_, _) => changes++;

        clock.Tick();

        Assert.Equal(0, changes);
    }

    [Fact]
    public void TimeChanged_IsRaisedOncePerChange()
    {
        using var clock = new MapClock(() => Start);

        var properties = new List<string?>();

        int changes = 0;

        clock.TimeChanged += (_, _) => changes++;

        clock.PropertyChanged += (_, e) => properties.Add(e.PropertyName);

        clock.Time = Start.AddHours(1);
        clock.Time = Start.AddHours(1);

        Assert.Equal(1, changes);
        Assert.Contains(nameof(MapClock.Time), properties);
    }

    [Fact]
    public void Time_IsStoredAsUtc()
    {
        using var clock = new MapClock(() => Start);

        clock.Time = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(DateTimeKind.Utc, clock.Time.Kind);
        Assert.Equal(12, clock.Time.Hour);
    }

    [Fact]
    public void InvalidSettings_Throw()
    {
        using var clock = new MapClock(() => Start);

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.TickInterval = TimeSpan.FromMilliseconds(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Speed = double.NaN);
    }

    [Fact]
    public void Timer_TicksOnItsOwnWhilePlaying()
    {
        using var clock = new MapClock(() => Start) { TickInterval = TimeSpan.FromMilliseconds(50) };

        using var ticked = new ManualResetEventSlim();

        clock.TimeChanged += (_, _) => ticked.Set();

        clock.IsPlaying = true;

        Assert.True(ticked.Wait(TimeSpan.FromSeconds(5)));

        clock.IsPlaying = false;
    }
}
