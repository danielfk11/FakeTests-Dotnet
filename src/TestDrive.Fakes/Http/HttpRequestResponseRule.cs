namespace TestDrive.Fakes.Http;

/// <summary>
/// Represents a rule for matching HTTP requests and generating responses.
/// </summary>
internal sealed class HttpRequestResponseRule
{
    /// <summary>
    /// Gets the predicate function that determines if this rule applies to a request.
    /// </summary>
    public Func<HttpRequestMessage, bool> Predicate { get; }

    /// <summary>
    /// Gets the response factory function that creates the HTTP response.
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestResponseRule"/> class.
    /// </summary>
    /// <param name="predicate">The predicate function for matching requests.</param>
    /// <param name="responseFactory">The response factory function.</param>
    public HttpRequestResponseRule(Func<HttpRequestMessage, bool> predicate, Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        ResponseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
    }
}