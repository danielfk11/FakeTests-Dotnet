namespace TestDrive.Fakes.Tests.Core;

public sealed class FixedClockTests
{
    [Fact]
    public void Constructor_WithInitialTime_SetsUtcNow()
    {
        // Arrange
        var initialTime = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var clock = new FixedClock(initialTime);

        // Assert
        clock.UtcNow.Should().Be(initialTime);
    }

    [Fact]
    public void Constructor_WithoutParameters_SetsUtcNowToCurrentTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var clock = new FixedClock();

        // Assert
        var after = DateTime.UtcNow;
        clock.UtcNow.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Advance_WithTimeSpan_AdvancesTime()
    {
        // Arrange
        var initialTime = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var clock = new FixedClock(initialTime);
        var advance = TimeSpan.FromHours(2);

        // Act
        clock.Advance(advance);

        // Assert
        clock.UtcNow.Should().Be(initialTime.Add(advance));
    }

    [Fact]
    public void Advance_MultipleTimeSpans_AccumulatesTime()
    {
        // Arrange
        var initialTime = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var clock = new FixedClock(initialTime);

        // Act
        clock.Advance(TimeSpan.FromMinutes(30));
        clock.Advance(TimeSpan.FromHours(1));
        clock.Advance(TimeSpan.FromSeconds(45));

        // Assert
        var expectedTime = initialTime
            .AddMinutes(30)
            .AddHours(1)
            .AddSeconds(45);
        clock.UtcNow.Should().Be(expectedTime);
    }

    [Fact]
    public void SetTime_WithNewTime_UpdatesUtcNow()
    {
        // Arrange
        var initialTime = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var newTime = new DateTime(2024, 6, 20, 15, 45, 30, DateTimeKind.Utc);
        var clock = new FixedClock(initialTime);

        // Act
        clock.SetTime(newTime);

        // Assert
        clock.UtcNow.Should().Be(newTime);
    }

    [Fact]
    public void UtcNow_IsThreadSafe()
    {
        // Arrange
        var clock = new FixedClock(DateTime.UtcNow);
        var tasks = new List<Task<DateTime>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                clock.Advance(TimeSpan.FromMilliseconds(1));
                return clock.UtcNow;
            }));
        }

        var results = Task.WhenAll(tasks).Result;

        // Assert
        results.Should().HaveCount(10);
        results.Distinct().Should().HaveCount(10); // All times should be different
    }
}