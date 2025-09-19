namespace TestDrive.Fakes;

internal static class ArgumentHelper
{
    public static void ThrowIfNull(object? argument, string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}