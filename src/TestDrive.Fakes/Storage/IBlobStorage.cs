namespace TestDrive.Fakes.Storage;

/// <summary>
/// Provides a contract for blob storage operations.
/// </summary>
public interface IBlobStorage
{
    /// <summary>
    /// Uploads content to the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="contentType">The optional content type. If null, defaults to "application/octet-stream".</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    Task UploadAsync(string bucket, string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads content from the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous download operation. Returns null if the object does not exist.</returns>
    Task<Stream?> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an object exists at the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if the object exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string bucket, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an object from the specified bucket and key.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="key">The object key.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous delete operation. Returns true if the object was deleted; false if it didn't exist.</returns>
    Task<bool> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all object keys in the specified bucket, optionally filtered by prefix.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="prefix">The optional prefix to filter keys. If null, all keys in the bucket are returned.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. Returns a read-only list of object keys.</returns>
    Task<IReadOnlyList<string>> ListKeysAsync(string bucket, string? prefix = null, CancellationToken cancellationToken = default);
}