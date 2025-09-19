namespace TestDrive.Fakes.Tests.Core;

public sealed class DeterministicIdGeneratorTests
{
    [Fact]
    public void GenerateId_FirstCall_Returns000001()
    {
        // Arrange
        var generator = new DeterministicIdGenerator();

        // Act
        var id = generator.GenerateId();

        // Assert
        id.Should().Be("000001");
    }

    [Fact]
    public void GenerateId_MultipleCalls_ReturnsSequentialIds()
    {
        // Arrange
        var generator = new DeterministicIdGenerator();

        // Act
        var ids = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            ids.Add(generator.GenerateId());
        }

        // Assert
        ids.Should().Equal("000001", "000002", "000003", "000004", "000005");
    }

    [Fact]
    public void Constructor_WithStartingValue_GeneratesFromNextValue()
    {
        // Arrange
        var generator = new DeterministicIdGenerator(100);

        // Act
        var id1 = generator.GenerateId();
        var id2 = generator.GenerateId();

        // Assert
        id1.Should().Be("000101");
        id2.Should().Be("000102");
    }

    [Fact]
    public void Reset_AfterGeneratingIds_RestartsFromOne()
    {
        // Arrange
        var generator = new DeterministicIdGenerator();
        generator.GenerateId(); // 000001
        generator.GenerateId(); // 000002

        // Act
        generator.Reset();
        var id = generator.GenerateId();

        // Assert
        id.Should().Be("000001");
    }

    [Fact]
    public void CurrentValue_ReturnsLastGeneratedNumber()
    {
        // Arrange
        var generator = new DeterministicIdGenerator();

        // Act & Assert
        generator.CurrentValue.Should().Be(0);

        generator.GenerateId();
        generator.CurrentValue.Should().Be(1);

        generator.GenerateId();
        generator.CurrentValue.Should().Be(2);
    }

    [Fact]
    public void GenerateId_WithLargeNumbers_FormatsCorrectly()
    {
        // Arrange
        var generator = new DeterministicIdGenerator(999998);

        // Act
        var id1 = generator.GenerateId();
        var id2 = generator.GenerateId();
        var id3 = generator.GenerateId();

        // Assert
        id1.Should().Be("999999");
        id2.Should().Be("1000000");
        id3.Should().Be("1000001");
    }

    [Fact]
    public void GenerateId_IsThreadSafe()
    {
        // Arrange
        var generator = new DeterministicIdGenerator();
        var tasks = new List<Task<string>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => generator.GenerateId()));
        }

        var results = Task.WhenAll(tasks).Result;

        // Assert
        results.Should().HaveCount(100);
        results.Distinct().Should().HaveCount(100); // All IDs should be unique
        results.Should().Contain("000001");
        results.Should().Contain("000100");
    }
}