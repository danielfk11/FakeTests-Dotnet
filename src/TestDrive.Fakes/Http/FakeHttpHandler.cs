using System.Collections.Concurrent;
using System.Net;
using System.Text;
using TestDrive.Fakes.Core;

namespace TestDrive.Fakes.Http;

/// <summary>
/// A fake HTTP message handler that allows configuring response rules for testing HTTP clients.
/// Useful for testing HTTP-dependent code without making actual network requests.
/// </summary>
public sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpRequestResponseRule> _rules = new();
    private readonly FaultPolicy _faultPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeHttpHandler"/> class.
    /// </summary>
    /// <param name="faultPolicy">The fault policy to apply to HTTP requests. If null, no faults will be introduced.</param>
    public FakeHttpHandler(FaultPolicy? faultPolicy = null)
    {
        _faultPolicy = faultPolicy ?? new FaultPolicy();
    }

    /// <summary>
    /// Adds a rule for handling HTTP requests that match the specified predicate.
    /// Rules are evaluated in the order they were added, with the last matching rule taking precedence.
    /// </summary>
    /// <param name="predicate">A function that determines whether this rule applies to a given request.</param>
    /// <param name="responseFactory">A function that creates the HTTP response for matching requests.</param>
    /// <returns>The current instance for method chaining.</returns>
    public FakeHttpHandler When(Func<HttpRequestMessage, bool> predicate, Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ArgumentHelper.ThrowIfNull(predicate, nameof(predicate));
        ArgumentHelper.ThrowIfNull(responseFactory, nameof(responseFactory));

        var rule = new HttpRequestResponseRule(predicate, responseFactory);
        _rules.Enqueue(rule);
        return this;
    }

    /// <summary>
    /// Adds a rule for handling HTTP requests to a specific URL.
    /// </summary>
    /// <param name="url">The URL to match (case-insensitive).</param>
    /// <param name="responseFactory">A function that creates the HTTP response.</param>
    /// <returns>The current instance for method chaining.</returns>
    public FakeHttpHandler When(string url, Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ArgumentHelper.ThrowIfNull(url, nameof(url));
        return When(req => string.Equals(req.RequestUri?.ToString(), url, StringComparison.OrdinalIgnoreCase), responseFactory);
    }

    /// <summary>
    /// Adds a rule for handling HTTP requests with a specific method and URL.
    /// </summary>
    /// <param name="method">The HTTP method to match.</param>
    /// <param name="url">The URL to match (case-insensitive).</param>
    /// <param name="responseFactory">A function that creates the HTTP response.</param>
    /// <returns>The current instance for method chaining.</returns>
    public FakeHttpHandler When(HttpMethod method, string url, Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ArgumentHelper.ThrowIfNull(method, nameof(method));
        ArgumentHelper.ThrowIfNull(url, nameof(url));
        return When(req => req.Method == method && 
                          string.Equals(req.RequestUri?.ToString(), url, StringComparison.OrdinalIgnoreCase), 
                   responseFactory);
    }

    /// <summary>
    /// Adds a rule for handling GET requests to a specific URL with a JSON response.
    /// </summary>
    /// <param name="url">The URL to match.</param>
    /// <param name="jsonResponse">The JSON response content.</param>
    /// <param name="statusCode">The HTTP status code to return. Defaults to OK.</param>
    /// <returns>The current instance for method chaining.</returns>
    public FakeHttpHandler WhenGet(string url, string jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return When(HttpMethod.Get, url, _ => CreateJsonResponse(jsonResponse, statusCode));
    }

    /// <summary>
    /// Adds a rule for handling POST requests to a specific URL with a JSON response.
    /// </summary>
    /// <param name="url">The URL to match.</param>
    /// <param name="jsonResponse">The JSON response content.</param>
    /// <param name="statusCode">The HTTP status code to return. Defaults to OK.</param>
    /// <returns>The current instance for method chaining.</returns>
    public FakeHttpHandler WhenPost(string url, string jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return When(HttpMethod.Post, url, _ => CreateJsonResponse(jsonResponse, statusCode));
    }

    /// <summary>
    /// Clears all configured rules.
    /// </summary>
    public void ClearRules()
    {
        while (_rules.TryDequeue(out _))
        {
            // Keep dequeuing until the queue is empty
        }
    }

    /// <summary>
    /// Gets the number of configured rules.
    /// </summary>
    public int RuleCount => _rules.Count;

    /// <summary>
    /// Sends an HTTP request asynchronously by applying configured rules.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation and contains the HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentHelper.ThrowIfNull(request, nameof(request));

        // Apply fault policy
        await _faultPolicy.ApplyAsync(cancellationToken).ConfigureAwait(false);

        // Find the last matching rule (rules are processed in reverse order for last-wins behavior)
        var rules = _rules.ToArray();
        for (int i = rules.Length - 1; i >= 0; i--)
        {
            var rule = rules[i];
            if (rule.Predicate(request))
            {
                try
                {
                    return rule.ResponseFactory(request);
                }
                catch (Exception ex)
                {
                    // If the response factory throws, return a 500 Internal Server Error
                    return CreateErrorResponse(HttpStatusCode.InternalServerError, $"Rule execution failed: {ex.Message}");
                }
            }
        }

        // No rule matched - return 404 Not Found
        return CreateErrorResponse(HttpStatusCode.NotFound, "No rule matched the request");
    }

    private static HttpResponseMessage CreateJsonResponse(string jsonContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        return response;
    }

    private static HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(message, Encoding.UTF8, "text/plain")
        };
        return response;
    }

    /// <summary>
    /// Releases the unmanaged resources and disposes of the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearRules();
        }
        base.Dispose(disposing);
    }
}