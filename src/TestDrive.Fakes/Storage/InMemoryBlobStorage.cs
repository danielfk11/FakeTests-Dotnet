using System.Collections.Concurrent;
using TestDrive.Fakes.Core;

namespace TestDrive.Fakes.Storage;

/// <summary>
/// An in-memory implementation of <see cref="IBlobStorage"/> for testing purposes.
/// Stores all blob data in memory using a thread-safe dictionary structure.
/// </summary>
public sealed class InMemoryBlobStorage : IBlobStorage
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BlobObject>> _storage = new();
    private readonly IClock _clock;
    private readonly FaultPolicy _faultPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobStorage"/> class.
    /// </summary>
    /// <param name="clock">The clock to use for timestamping uploads. If null, a default FixedClock will be used.</param>
    /// <param name="faultPolicy">The fault policy to apply to operations. If null, no faults will be introduced.</param>
    public InMemoryBlobStorage(IClock? clock = null, FaultPolicy? faultPolicy = null)
    {
        _clock = clock ?? new FixedClock();
        _faultPolicy = faultPolicy ?? new FaultPolicy();
    }

    /// <summary>
    /// Uploads content to the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="contentType">The optional content type. If null, defaults to "application/octet-stream".</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    public async Task UploadAsync(string bucket, string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));
        ArgumentHelper.ThrowIfNull(content, nameof(content));

        await _faultPolicy.ApplyAsync(cancellationToken).ConfigureAwait(false);

        contentType ??= "application/octet-stream";

        // Read content into byte array
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        var contentBytes = memoryStream.ToArray();

        // Store the blob
        var bucketStorage = _storage.GetOrAdd(bucket, _ => new ConcurrentDictionary<string, BlobObject>());
        var blobObject = new BlobObject(contentBytes, contentType, _clock.UtcNow);
        bucketStorage.AddOrUpdate(key, blobObject, (_, _) => blobObject);
    }

    /// <summary>
    /// Downloads content from the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous download operation. Returns null if the object does not exist.</returns>
    public async Task<Stream?> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));

        await _faultPolicy.ApplyAsync(cancellationToken).ConfigureAwait(false);

        if (!_storage.TryGetValue(bucket, out var bucketStorage) ||
            !bucketStorage.TryGetValue(key, out var blobObject))
        {
            return null;
        }

        return blobObject.ToStream();
    }

    /// <summary>
    /// Checks whether an object exists at the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if the object exists; otherwise, false.</returns>
    public async Task<bool> ExistsAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));

        await _faultPolicy.ApplyAsync(cancellationToken).ConfigureAwait(false);

        return _storage.TryGetValue(bucket, out var bucketStorage) &&
               bucketStorage.ContainsKey(key);
    }

    /// <summary>
    /// Deletes an object from the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous delete operation. Returns true if the object was deleted; false if it didn't exist.</returns>
    public async Task<bool> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));
        ArgumentHelper.ThrowIfNull(key, nameof(key));

        await _faultPolicy.ApplyAsync(cancellationToken).ConfigureAwait(false);

        if (!_storage.TryGetValue(bucket, out var bucketStorage))
        {
            return false;
        }

        return bucketStorage.TryRemove(key, out _);
    }

    /// <summary>
    /// Lists all object keys in the specified bucket, optionally filtered by prefix.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="prefix">The optional prefix to filter keys. If null, all keys in the bucket are returned.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. Returns a read-only list of object keys.</returns>
    public async Task<IReadOnlyList<string>> ListKeysAsync(string bucket, string? prefix = null, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));

        await _faultPolicy.ApplyAsync(cancellationToken).ConfigureAwait(false);

        if (!_storage.TryGetValue(bucket, out var bucketStorage))
        {
            return Array.Empty<string>();
        }

        var keys = bucketStorage.Keys.AsEnumerable();

        if (!string.IsNullOrEmpty(prefix))
        {
            keys = keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        return keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Gets all blob objects in the specified bucket.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <returns>A read-only dictionary of all blobs in the bucket, keyed by object key.</returns>
    public IReadOnlyDictionary<string, BlobObject> GetAllBlobs(string bucket)
    {
        ArgumentHelper.ThrowIfNull(bucket, nameof(bucket));

        if (!_storage.TryGetValue(bucket, out var bucketStorage))
        {
            return new Dictionary<string, BlobObject>();
        }

        return new Dictionary<string, BlobObject>(bucketStorage);
    }

    /// <summary>
    /// Gets the names of all buckets in the storage.
    /// </summary>
    /// <returns>A read-only list of bucket names.</returns>
    public IReadOnlyList<string> GetBucketNames()
    {
        return _storage.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Clears all data from the storage.
    /// </summary>
    public void Clear()
    {
        _storage.Clear();
    }

    /// <summary>
    /// Gets the total number of objects across all buckets.
    /// </summary>
    public int TotalObjectCount => _storage.Values.Sum(bucket => bucket.Count);

    /// <summary>
    /// Gets the total size of all stored objects in bytes.
    /// </summary>
    public long TotalSizeInBytes => _storage.Values
        .SelectMany(bucket => bucket.Values)
        .Sum(blob => blob.Size);
}