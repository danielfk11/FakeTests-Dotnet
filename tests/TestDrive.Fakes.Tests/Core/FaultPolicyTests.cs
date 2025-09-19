using System.Diagnostics;

namespace TestDrive.Fakes.Tests.Core;

public sealed class FaultPolicyTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithNoFaults()
    {
        // Act
        var policy = new FaultPolicy();

        // Assert
        policy.FailureProbability.Should().Be(0.0);
        policy.FixedLatency.Should().BeNull();
        policy.ExceptionFactory.Should().BeNull();
    }

    [Fact]
    public async Task ApplyAsync_WithNoFaults_CompletesWithoutError()
    {
        // Arrange
        var policy = new FaultPolicy();

        // Act
        var act = async () => await policy.ApplyAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyAsync_WithFixedLatency_IntroducesDelay()
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(100);
        var policy = new FaultPolicy { FixedLatency = latency };
        var stopwatch = Stopwatch.StartNew();

        // Act
        await policy.ApplyAsync();

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(80); // Allow for some timing variance
    }

    [Fact]
    public async Task ApplyAsync_WithFailureProbability1_AlwaysThrows()
    {
        // Arrange
        var policy = new FaultPolicy { FailureProbability = 1.0 };

        // Act
        var act = async () => await policy.ApplyAsync();

        // Assert
        await act.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("Fault policy triggered a failure");
    }

    [Fact]
    public async Task ApplyAsync_WithFailureProbability0_NeverThrows()
    {
        // Arrange
        var policy = new FaultPolicy { FailureProbability = 0.0 };

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            var act = async () => await policy.ApplyAsync();
            await act.Should().NotThrowAsync();
        }
    }

    [Fact]
    public async Task ApplyAsync_WithCustomExceptionFactory_ThrowsCustomException()
    {
        // Arrange
        var customException = new ArgumentException("Custom test exception");
        var policy = new FaultPolicy
        {
            FailureProbability = 1.0,
            ExceptionFactory = () => customException
        };

        // Act
        var act = async () => await policy.ApplyAsync();

        // Assert
        await act.Should().ThrowExactlyAsync<ArgumentException>()
            .WithMessage("Custom test exception");
    }

    [Fact]
    public async Task ApplyAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var policy = new FaultPolicy { FixedLatency = TimeSpan.FromSeconds(10) };
        using var cts = new CancellationTokenSource();

        // Act
        var task = policy.ApplyAsync(cts.Token);
        cts.Cancel();

        // Assert
        var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Apply_Synchronous_WithFixedLatency_IntroducesDelay()
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(50);
        var policy = new FaultPolicy { FixedLatency = latency };
        var stopwatch = Stopwatch.StartNew();

        // Act
        policy.Apply();

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(40); // Allow for some timing variance
    }

    [Fact]
    public void Apply_Synchronous_WithFailureProbability1_AlwaysThrows()
    {
        // Arrange
        var policy = new FaultPolicy { FailureProbability = 1.0 };

        // Act
        var act = () => policy.Apply();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Fault policy triggered a failure");
    }

    [Fact]
    public void AlwaysFail_CreatesAlwaysFailingPolicy()
    {
        // Act
        var policy = FaultPolicy.AlwaysFail();

        // Assert
        policy.FailureProbability.Should().Be(1.0);
        var act = () => policy.Apply();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AlwaysFail_WithCustomException_UsesCustomException()
    {
        // Arrange
        var customException = new NotSupportedException("Test exception");

        // Act
        var policy = FaultPolicy.AlwaysFail(() => customException);

        // Assert
        var act = () => policy.Apply();
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public void WithLatency_CreatesLatencyPolicy()
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(100);

        // Act
        var policy = FaultPolicy.WithLatency(latency);

        // Assert
        policy.FixedLatency.Should().Be(latency);
        policy.FailureProbability.Should().Be(0.0);
    }

    [Fact]
    public void WithFailureProbability_CreatesFailurePolicy()
    {
        // Act
        var policy = FaultPolicy.WithFailureProbability(0.5);

        // Assert
        policy.FailureProbability.Should().Be(0.5);
    }

    [Fact]
    public void WithFailureProbability_WithInvalidProbability_ThrowsException()
    {
        // Act & Assert
        var act1 = () => FaultPolicy.WithFailureProbability(-0.1);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => FaultPolicy.WithFailureProbability(1.1);
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ApplyAsync_WithPredictableRandom_BehavesConsistently()
    {
        // Arrange
        var random = new Random(42); // Fixed seed for predictable results
        var policy = new FaultPolicy
        {
            FailureProbability = 0.5,
            Random = random
        };

        // Act & Assert
        var results = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            try
            {
                await policy.ApplyAsync();
                results.Add(false); // No exception
            }
            catch (InvalidOperationException)
            {
                results.Add(true); // Exception thrown
            }
        }

        // Should have both successes and failures with roughly 50% probability
        var failureCount = results.Count(r => r);
        failureCount.Should().BeGreaterThan(30).And.BeLessThan(70);
    }
}