namespace TestDrive.Fakes.Core;

/// <summary>
/// Provides a contract for retrieving the current time.
/// Useful for making time-dependent code testable and deterministic.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}