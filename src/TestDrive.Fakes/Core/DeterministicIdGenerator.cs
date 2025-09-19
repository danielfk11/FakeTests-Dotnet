namespace TestDrive.Fakes.Core;

/// <summary>
/// A deterministic implementation of <see cref="IIdGenerator"/> that generates sequential IDs.
/// Useful for creating predictable and testable identifier sequences.
/// </summary>
public sealed class DeterministicIdGenerator : IIdGenerator
{
    private long _counter;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicIdGenerator"/> class starting from 1.
    /// </summary>
    public DeterministicIdGenerator()
    {
        _counter = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicIdGenerator"/> class with a specified starting value.
    /// </summary>
    /// <param name="startingValue">The initial value for the counter (the first generated ID will be startingValue + 1).</param>
    public DeterministicIdGenerator(long startingValue)
    {
        _counter = startingValue;
    }

    /// <summary>
    /// Generates the next sequential ID in the format "000001", "000002", etc.
    /// </summary>
    /// <returns>A six-digit zero-padded sequential identifier.</returns>
    public string GenerateId()
    {
        lock (_lock)
        {
            _counter++;
            return _counter.ToString("D6");
        }
    }

    /// <summary>
    /// Resets the counter to start generating IDs from "000001" again.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _counter = 0;
        }
    }

    /// <summary>
    /// Gets the current counter value (the last generated ID number).
    /// </summary>
    public long CurrentValue
    {
        get
        {
            lock (_lock)
            {
                return _counter;
            }
        }
    }
}