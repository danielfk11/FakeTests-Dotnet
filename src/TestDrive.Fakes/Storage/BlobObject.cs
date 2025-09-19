namespace TestDrive.Fakes.Storage;

/// <summary>
/// Represents a blob object stored in memory.
/// </summary>
public sealed class BlobObject
{
    /// <summary>
    /// Gets the blob content as a byte array.
    /// </summary>
    public byte[] Content { get; }

    /// <summary>
    /// Gets the content type of the blob.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the UTC timestamp when the blob was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobObject"/> class.
    /// </summary>
    /// <param name="content">The blob content.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="uploadedAt">The upload timestamp.</param>
    public BlobObject(byte[] content, string contentType, DateTime uploadedAt)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        UploadedAt = uploadedAt;
    }

    /// <summary>
    /// Gets the size of the blob content in bytes.
    /// </summary>
    public long Size => Content.Length;

    /// <summary>
    /// Creates a stream containing the blob content.
    /// </summary>
    /// <returns>A new memory stream with the blob content.</returns>
    public Stream ToStream()
    {
        return new MemoryStream(Content);
    }
}