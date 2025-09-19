using System.Net;

namespace TestDrive.Fakes.Tests.Http;

public sealed class FakeHttpHandlerTests
{
    [Fact]
    public async Task SendAsync_WithNoRules_Returns404NotFound()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://example.com/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("No rule matched the request");
    }

    [Fact]
    public async Task SendAsync_WithMatchingRule_ReturnsConfiguredResponse()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.When(req => req.RequestUri!.ToString().Contains("test"),
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("Success")
                    });

        var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://example.com/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Success");
    }

    [Fact]
    public async Task SendAsync_WithMultipleRules_UsesLastMatchingRule()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.When(req => true, req => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("First rule")
                })
                .When(req => req.RequestUri!.ToString().Contains("test"),
                     req => new HttpResponseMessage(HttpStatusCode.Accepted)
                     {
                         Content = new StringContent("Second rule")
                     });

        var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://example.com/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Second rule");
    }

    [Fact]
    public async Task When_WithUrl_MatchesExactUrl()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.When("https://api.example.com/users",
                    req => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("Users endpoint")
                    });

        var client = new HttpClient(handler);

        // Act
        var response1 = await client.GetAsync("https://api.example.com/users");
        var response2 = await client.GetAsync("https://api.example.com/posts");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var content1 = await response1.Content.ReadAsStringAsync();
        content1.Should().Be("Users endpoint");

        response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task When_WithMethodAndUrl_MatchesMethodAndUrl()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.When(HttpMethod.Post, "https://api.example.com/users",
                    req => new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent("User created")
                    });

        var client = new HttpClient(handler);

        // Act
        var getResponse = await client.GetAsync("https://api.example.com/users");
        var postResponse = await client.PostAsync("https://api.example.com/users", new StringContent(""));

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var postContent = await postResponse.Content.ReadAsStringAsync();
        postContent.Should().Be("User created");
    }

    [Fact]
    public async Task WhenGet_CreatesGetRuleWithJsonResponse()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.WhenGet("https://api.example.com/data", """{"message": "Hello World"}""");

        var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://api.example.com/data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("""{"message": "Hello World"}""");
    }

    [Fact]
    public async Task WhenPost_CreatesPostRuleWithJsonResponse()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.WhenPost("https://api.example.com/data", """{"result": "success"}""", HttpStatusCode.Created);

        var client = new HttpClient(handler);

        // Act
        var response = await client.PostAsync("https://api.example.com/data", new StringContent(""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("""{"result": "success"}""");
    }

    [Fact]
    public async Task SendAsync_WhenResponseFactoryThrows_Returns500()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.When(req => true, req => throw new InvalidOperationException("Factory error"));

        var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://example.com/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Rule execution failed: Factory error");
    }

    [Fact]
    public void ClearRules_RemovesAllRules()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.When(req => true, req => new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        handler.ClearRules();

        // Assert
        handler.RuleCount.Should().Be(0);
    }

    [Fact]
    public void RuleCount_ReturnsCorrectCount()
    {
        // Arrange
        var handler = new FakeHttpHandler();

        // Act & Assert
        handler.RuleCount.Should().Be(0);

        handler.When(req => true, req => new HttpResponseMessage(HttpStatusCode.OK));
        handler.RuleCount.Should().Be(1);

        handler.When(req => true, req => new HttpResponseMessage(HttpStatusCode.OK));
        handler.RuleCount.Should().Be(2);

        handler.ClearRules();
        handler.RuleCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WithFaultPolicy_AppliesFaultPolicy()
    {
        // Arrange
        var faultPolicy = FaultPolicy.AlwaysFail(() => new InvalidOperationException("HTTP fault"));
        var handler = new FakeHttpHandler(faultPolicy);
        var client = new HttpClient(handler);

        // Act & Assert
        await client.Invoking(c => c.GetAsync("https://example.com/test"))
            .Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("HTTP fault");
    }

    [Fact]
    public async Task SendAsync_WithLatencyFaultPolicy_IntroducesDelay()
    {
        // Arrange
        var faultPolicy = FaultPolicy.WithLatency(TimeSpan.FromMilliseconds(50));
        var handler = new FakeHttpHandler(faultPolicy);
        handler.WhenGet("https://example.com/test", """{"ok": true}""");

        var client = new HttpClient(handler);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await client.GetAsync("https://example.com/test");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(40); // Allow for timing variance
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var faultPolicy = FaultPolicy.WithLatency(TimeSpan.FromSeconds(10));
        var handler = new FakeHttpHandler(faultPolicy);
        var client = new HttpClient(handler);

        using var cts = new CancellationTokenSource();

        // Act
        var task = client.GetAsync("https://example.com/test", cts.Token);
        cts.Cancel();

        // Assert
        var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ClearsRules()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        handler.When(req => true, req => new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        handler.Dispose();

        // Assert
        handler.RuleCount.Should().Be(0);
    }
}