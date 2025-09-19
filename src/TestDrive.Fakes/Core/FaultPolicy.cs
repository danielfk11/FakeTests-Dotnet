namespace TestDrive.Fakes.Core;

/// <summary>
/// Represents a policy for introducing faults (latency and failures) into operations.
/// Useful for testing resilience and error handling in applications.
/// </summary>
public sealed class FaultPolicy
{
    private static readonly Random DefaultRandom = new();

    /// <summary>
    /// Gets or sets the probability of failure (0.0 = never fail, 1.0 = always fail).
    /// </summary>
    public double FailureProbability { get; set; }

    /// <summary>
    /// Gets or sets the fixed latency to introduce before each operation.
    /// </summary>
    public TimeSpan? FixedLatency { get; set; }

    /// <summary>
    /// Gets or sets a factory function for creating exceptions when a failure occurs.
    /// If null, a default <see cref="InvalidOperationException"/> will be thrown.
    /// </summary>
    public Func<Exception>? ExceptionFactory { get; set; }

    /// <summary>
    /// Gets or sets the random number generator used for probability calculations.
    /// If not set, a shared static instance will be used.
    /// </summary>
    public Random? Random { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultPolicy"/> class with no faults.
    /// </summary>
    public FaultPolicy()
    {
        FailureProbability = 0.0;
    }

    /// <summary>
    /// Creates a fault policy that always fails with the specified exception factory.
    /// </summary>
    /// <param name="exceptionFactory">The factory function for creating exceptions.</param>
    /// <returns>A fault policy that always fails.</returns>
    public static FaultPolicy AlwaysFail(Func<Exception>? exceptionFactory = null)
    {
        return new FaultPolicy
        {
            FailureProbability = 1.0,
            ExceptionFactory = exceptionFactory
        };
    }

    /// <summary>
    /// Creates a fault policy that introduces a fixed latency.
    /// </summary>
    /// <param name="latency">The latency to introduce.</param>
    /// <returns>A fault policy with fixed latency.</returns>
    public static FaultPolicy WithLatency(TimeSpan latency)
    {
        return new FaultPolicy
        {
            FixedLatency = latency
        };
    }

    /// <summary>
    /// Creates a fault policy with a specific failure probability.
    /// </summary>
    /// <param name="probability">The probability of failure (0.0 to 1.0).</param>
    /// <param name="exceptionFactory">Optional factory function for creating exceptions.</param>
    /// <returns>A fault policy with the specified failure probability.</returns>
    public static FaultPolicy WithFailureProbability(double probability, Func<Exception>? exceptionFactory = null)
    {
        if (probability < 0.0 || probability > 1.0)
            throw new ArgumentException("Probability must be between 0.0 and 1.0", nameof(probability));

        return new FaultPolicy
        {
            FailureProbability = probability,
            ExceptionFactory = exceptionFactory
        };
    }

    /// <summary>
    /// Applies the fault policy asynchronously, potentially introducing latency and/or throwing an exception.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <exception cref="Exception">Thrown when the fault policy determines that a failure should occur.</exception>
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        // Apply latency if configured
        if (FixedLatency.HasValue && FixedLatency.Value > TimeSpan.Zero)
        {
            await Task.Delay(FixedLatency.Value, cancellationToken).ConfigureAwait(false);
        }

        // Check for failure
        if (FailureProbability > 0.0)
        {
            var random = Random ?? DefaultRandom;
            var randomValue = random.NextDouble();

            if (randomValue < FailureProbability)
            {
                var exception = ExceptionFactory?.Invoke() ?? 
                    new InvalidOperationException("Fault policy triggered a failure");
                throw exception;
            }
        }
    }

    /// <summary>
    /// Applies the fault policy synchronously, potentially introducing latency and/or throwing an exception.
    /// Note: This method will block the calling thread for the duration of any configured latency.
    /// </summary>
    /// <exception cref="Exception">Thrown when the fault policy determines that a failure should occur.</exception>
    public void Apply()
    {
        // Apply latency if configured
        if (FixedLatency.HasValue && FixedLatency.Value > TimeSpan.Zero)
        {
            Thread.Sleep(FixedLatency.Value);
        }

        // Check for failure
        if (FailureProbability > 0.0)
        {
            var random = Random ?? DefaultRandom;
            var randomValue = random.NextDouble();

            if (randomValue < FailureProbability)
            {
                var exception = ExceptionFactory?.Invoke() ?? 
                    new InvalidOperationException("Fault policy triggered a failure");
                throw exception;
            }
        }
    }
}