namespace TestDrive.Fakes.Core;

/// <summary>
/// A deterministic implementation of <see cref="IClock"/> that allows manual time advancement.
/// Useful for testing time-dependent behavior in a controlled manner.
/// </summary>
public sealed class FixedClock : IClock
{
    private DateTime _currentTime;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedClock"/> class with the specified initial time.
    /// </summary>
    /// <param name="initialTime">The initial UTC time for the clock.</param>
    public FixedClock(DateTime initialTime)
    {
        _currentTime = initialTime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedClock"/> class with the current UTC time.
    /// </summary>
    public FixedClock() : this(DateTime.UtcNow)
    {
    }

    /// <summary>
    /// Gets the current UTC time as set by this fixed clock.
    /// </summary>
    public DateTime UtcNow
    {
        get
        {
            lock (_lock)
            {
                return _currentTime;
            }
        }
    }

    /// <summary>
    /// Advances the clock by the specified time span.
    /// </summary>
    /// <param name="timeSpan">The amount of time to advance the clock by.</param>
    public void Advance(TimeSpan timeSpan)
    {
        lock (_lock)
        {
            _currentTime = _currentTime.Add(timeSpan);
        }
    }

    /// <summary>
    /// Sets the clock to the specified UTC time.
    /// </summary>
    /// <param name="utcTime">The UTC time to set the clock to.</param>
    public void SetTime(DateTime utcTime)
    {
        lock (_lock)
        {
            _currentTime = utcTime;
        }
    }
}