using TestDrive.Fakes.Storage;

namespace TestDrive.Fakes.TestKit;

/// <summary>
/// Provides extension methods for making assertions about blob storage in tests.
/// </summary>
public static class StorageAssertions
{
    /// <summary>
    /// Asserts that the storage contains a blob at the specified bucket and key.
    /// </summary>
    /// <param name="storage">The in-memory blob storage to check.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldContainBlob(this InMemoryBlobStorage storage, string bucket, string key)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));

        var exists = storage.ExistsAsync(bucket, key).GetAwaiter().GetResult();
        if (!exists)
        {
            throw new InvalidOperationException(
                $"Expected blob to exist at bucket '{bucket}' with key '{key}', but it was not found.");
        }
    }

    /// <summary>
    /// Asserts that the storage does not contain a blob at the specified bucket and key.
    /// </summary>
    /// <param name="storage">The in-memory blob storage to check.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldNotContainBlob(this InMemoryBlobStorage storage, string bucket, string key)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));

        var exists = storage.ExistsAsync(bucket, key).GetAwaiter().GetResult();
        if (exists)
        {
            throw new InvalidOperationException(
                $"Expected blob not to exist at bucket '{bucket}' with key '{key}', but it was found.");
        }
    }

    /// <summary>
    /// Asserts that the storage contains the specified number of objects in a bucket.
    /// </summary>
    /// <param name="storage">The in-memory blob storage to check.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="expectedCount">The expected number of objects.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveObjectCount(this InMemoryBlobStorage storage, string bucket, int expectedCount)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));

        var keys = storage.ListKeysAsync(bucket).GetAwaiter().GetResult();
        var actualCount = keys.Count;

        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected bucket '{bucket}' to contain {expectedCount} objects, but it contains {actualCount}.");
        }
    }

    /// <summary>
    /// Asserts that a blob in the storage has the specified content type.
    /// </summary>
    /// <param name="storage">The in-memory blob storage to check.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="expectedContentType">The expected content type.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveContentType(this InMemoryBlobStorage storage, string bucket, string key, string expectedContentType)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));
        ArgumentHelper.ThrowIfNull(expectedContentType, nameof(expectedContentType));

        var blobs = storage.GetAllBlobs(bucket);
        if (!blobs.TryGetValue(key, out var blob))
        {
            throw new InvalidOperationException(
                $"Blob not found at bucket '{bucket}' with key '{key}'.");
        }

        if (!string.Equals(blob.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Expected blob content type to be '{expectedContentType}', but it was '{blob.ContentType}'.");
        }
    }

    /// <summary>
    /// Asserts that a blob in the storage has content that matches the specified byte array.
    /// </summary>
    /// <param name="storage">The in-memory blob storage to check.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="expectedContent">The expected content as a byte array.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveContent(this InMemoryBlobStorage storage, string bucket, string key, byte[] expectedContent)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));
        ArgumentHelper.ThrowIfNull(expectedContent, nameof(expectedContent));

        var blobs = storage.GetAllBlobs(bucket);
        if (!blobs.TryGetValue(key, out var blob))
        {
            throw new InvalidOperationException(
                $"Blob not found at bucket '{bucket}' with key '{key}'.");
        }

        if (!blob.Content.SequenceEqual(expectedContent))
        {
            throw new InvalidOperationException(
                $"Blob content does not match expected content. Expected {expectedContent.Length} bytes, got {blob.Content.Length} bytes.");
        }
    }

    /// <summary>
    /// Asserts that a blob in the storage has content that matches the specified string (UTF-8 encoded).
    /// </summary>
    /// <param name="storage">The in-memory blob storage to check.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="expectedContent">The expected content as a string.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveTextContent(this InMemoryBlobStorage storage, string bucket, string key, string expectedContent)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));
        ArgumentHelper.ThrowIfNull(expectedContent, nameof(expectedContent));

        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expectedContent);
        storage.ShouldHaveContent(bucket, key, expectedBytes);
    }

    /// <summary>
    /// Gets the blob object at the specified bucket and key, or throws an exception if not found.
    /// </summary>
    /// <param name="storage">The in-memory blob storage to search.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <returns>The blob object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the blob is not found.</exception>
    public static BlobObject GetBlob(this InMemoryBlobStorage storage, string bucket, string key)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));

        var blobs = storage.GetAllBlobs(bucket);
        if (!blobs.TryGetValue(key, out var blob))
        {
            throw new InvalidOperationException(
                $"Blob not found at bucket '{bucket}' with key '{key}'.");
        }

        return blob;
    }

    /// <summary>
    /// Gets the text content of a blob (assuming UTF-8 encoding), or throws an exception if not found.
    /// </summary>
    /// <param name="storage">The in-memory blob storage to search.</param>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <returns>The blob content as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the blob is not found.</exception>
    public static string GetBlobTextContent(this InMemoryBlobStorage storage, string bucket, string key)
    {
        ArgumentHelper.ThrowIfNull(storage, nameof(storage));
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));

        var blob = storage.GetBlob(bucket, key);
        return System.Text.Encoding.UTF8.GetString(blob.Content);
    }
}