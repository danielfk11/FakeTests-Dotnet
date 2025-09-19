namespace TestDrive.Fakes.Tests.Storage;

public sealed class InMemoryBlobStorageTests
{
    [Fact]
    public async Task UploadAsync_WithValidParameters_StoresBlob()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        var content = "Test content"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        await storage.UploadAsync("bucket1", "key1", stream, "text/plain");

        // Assert
        var exists = await storage.ExistsAsync("bucket1", "key1");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream = new MemoryStream();

        // Act & Assert
        await storage.Invoking(s => s.UploadAsync(null!, "key", stream))
            .Should().ThrowExactlyAsync<ArgumentNullException>();

        await storage.Invoking(s => s.UploadAsync("bucket", null!, stream))
            .Should().ThrowExactlyAsync<ArgumentNullException>();

        await storage.Invoking(s => s.UploadAsync("bucket", "key", null!))
            .Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DownloadAsync_ExistingBlob_ReturnsContent()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        var originalContent = "Test content"u8.ToArray();
        using var uploadStream = new MemoryStream(originalContent);
        await storage.UploadAsync("bucket1", "key1", uploadStream);

        // Act
        using var downloadStream = await storage.DownloadAsync("bucket1", "key1");

        // Assert
        downloadStream.Should().NotBeNull();
        using var memoryStream = new MemoryStream();
        await downloadStream!.CopyToAsync(memoryStream);
        memoryStream.ToArray().Should().Equal(originalContent);
    }

    [Fact]
    public async Task DownloadAsync_NonExistentBlob_ReturnsNull()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();

        // Act
        var result = await storage.DownloadAsync("bucket1", "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ExistingBlob_ReturnsTrue()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream = new MemoryStream("content"u8.ToArray());
        await storage.UploadAsync("bucket1", "key1", stream);

        // Act
        var exists = await storage.ExistsAsync("bucket1", "key1");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentBlob_ReturnsFalse()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();

        // Act
        var exists = await storage.ExistsAsync("bucket1", "key1");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingBlob_ReturnsTrue()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream = new MemoryStream("content"u8.ToArray());
        await storage.UploadAsync("bucket1", "key1", stream);

        // Act
        var deleted = await storage.DeleteAsync("bucket1", "key1");

        // Assert
        deleted.Should().BeTrue();
        var exists = await storage.ExistsAsync("bucket1", "key1");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentBlob_ReturnsFalse()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();

        // Act
        var deleted = await storage.DeleteAsync("bucket1", "key1");

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task ListKeysAsync_WithBlobs_ReturnsAllKeys()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream1 = new MemoryStream("content1"u8.ToArray());
        using var stream2 = new MemoryStream("content2"u8.ToArray());
        using var stream3 = new MemoryStream("content3"u8.ToArray());

        await storage.UploadAsync("bucket1", "key1", stream1);
        await storage.UploadAsync("bucket1", "key2", stream2);
        await storage.UploadAsync("bucket1", "subfolder/key3", stream3);

        // Act
        var keys = await storage.ListKeysAsync("bucket1");

        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain("key1");
        keys.Should().Contain("key2");
        keys.Should().Contain("subfolder/key3");
    }

    [Fact]
    public async Task ListKeysAsync_WithPrefix_ReturnsFilteredKeys()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream1 = new MemoryStream("content1"u8.ToArray());
        using var stream2 = new MemoryStream("content2"u8.ToArray());
        using var stream3 = new MemoryStream("content3"u8.ToArray());

        await storage.UploadAsync("bucket1", "photos/photo1.jpg", stream1);
        await storage.UploadAsync("bucket1", "photos/photo2.jpg", stream2);
        await storage.UploadAsync("bucket1", "documents/doc1.pdf", stream3);

        // Act
        var keys = await storage.ListKeysAsync("bucket1", "photos/");

        // Assert
        keys.Should().HaveCount(2);
        keys.Should().Contain("photos/photo1.jpg");
        keys.Should().Contain("photos/photo2.jpg");
    }

    [Fact]
    public async Task ListKeysAsync_EmptyBucket_ReturnsEmptyList()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();

        // Act
        var keys = await storage.ListKeysAsync("empty-bucket");

        // Assert
        keys.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsync_WithoutContentType_UsesDefaultContentType()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream = new MemoryStream("content"u8.ToArray());

        // Act
        await storage.UploadAsync("bucket1", "key1", stream);

        // Assert
        var blobs = storage.GetAllBlobs("bucket1");
        blobs["key1"].ContentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task UploadAsync_WithContentType_StoresContentType()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream = new MemoryStream("content"u8.ToArray());

        // Act
        await storage.UploadAsync("bucket1", "key1", stream, "application/json");

        // Assert
        var blobs = storage.GetAllBlobs("bucket1");
        blobs["key1"].ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task UploadAsync_WithClock_StoresUploadTimestamp()
    {
        // Arrange
        var fixedTime = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var clock = new FixedClock(fixedTime);
        var storage = new InMemoryBlobStorage(clock);
        using var stream = new MemoryStream("content"u8.ToArray());

        // Act
        await storage.UploadAsync("bucket1", "key1", stream);

        // Assert
        var blobs = storage.GetAllBlobs("bucket1");
        blobs["key1"].UploadedAt.Should().Be(fixedTime);
    }

    [Fact]
    public void GetBucketNames_WithMultipleBuckets_ReturnsAllBucketNames()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream1 = new MemoryStream("content"u8.ToArray());
        using var stream2 = new MemoryStream("content"u8.ToArray());

        storage.UploadAsync("bucket1", "key1", stream1).Wait();
        storage.UploadAsync("bucket2", "key2", stream2).Wait();

        // Act
        var buckets = storage.GetBucketNames();

        // Assert
        buckets.Should().HaveCount(2);
        buckets.Should().Contain("bucket1");
        buckets.Should().Contain("bucket2");
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        using var stream = new MemoryStream("content"u8.ToArray());
        storage.UploadAsync("bucket1", "key1", stream).Wait();

        // Act
        storage.Clear();

        // Assert
        storage.GetBucketNames().Should().BeEmpty();
        storage.TotalObjectCount.Should().Be(0);
    }

    [Fact]
    public async Task TotalObjectCount_ReturnsCorrectCount()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();

        // Act & Assert
        storage.TotalObjectCount.Should().Be(0);

        using var stream1 = new MemoryStream("content1"u8.ToArray());
        await storage.UploadAsync("bucket1", "key1", stream1);
        storage.TotalObjectCount.Should().Be(1);

        using var stream2 = new MemoryStream("content2"u8.ToArray());
        await storage.UploadAsync("bucket2", "key2", stream2);
        storage.TotalObjectCount.Should().Be(2);
    }

    [Fact]
    public async Task TotalSizeInBytes_ReturnsCorrectSize()
    {
        // Arrange
        var storage = new InMemoryBlobStorage();
        var content1 = "Hello"u8.ToArray(); // 5 bytes
        var content2 = "World!"u8.ToArray(); // 6 bytes

        // Act
        using var stream1 = new MemoryStream(content1);
        using var stream2 = new MemoryStream(content2);
        await storage.UploadAsync("bucket1", "key1", stream1);
        await storage.UploadAsync("bucket1", "key2", stream2);

        // Assert
        storage.TotalSizeInBytes.Should().Be(11);
    }

    [Fact]
    public async Task UploadAsync_WithFaultPolicy_AppliesFaultPolicy()
    {
        // Arrange
        var faultPolicy = FaultPolicy.AlwaysFail(() => new InvalidOperationException("Storage fault"));
        var storage = new InMemoryBlobStorage(faultPolicy: faultPolicy);
        using var stream = new MemoryStream("content"u8.ToArray());

        // Act & Assert
        await storage.Invoking(s => s.UploadAsync("bucket1", "key1", stream))
            .Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("Storage fault");
    }
}