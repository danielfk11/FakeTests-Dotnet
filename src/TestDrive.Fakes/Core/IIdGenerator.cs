namespace TestDrive.Fakes.Core;

/// <summary>
/// Provides a contract for generating unique identifiers.
/// Useful for making ID generation deterministic in tests.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates the next unique identifier.
    /// </summary>
    /// <returns>A unique identifier as a string.</returns>
    string GenerateId();
}