namespace TestDrive.Fakes.Tests.Email;

public sealed class FakeEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WithValidParameters_StoresEmailInOutbox()
    {
        // Arrange
        var clock = new FixedClock(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        var sender = new FakeEmailSender(clock);

        // Act
        await sender.SendAsync("from@test.com", "to@test.com", "Test Subject", "Test Body");

        // Assert
        sender.Outbox.Should().HaveCount(1);
        var email = sender.Outbox[0];
        email.From.Should().Be("from@test.com");
        email.To.Should().Be("to@test.com");
        email.Subject.Should().Be("Test Subject");
        email.Body.Should().Be("Test Body");
        email.UtcSentAt.Should().Be(clock.UtcNow);
    }

    [Fact]
    public async Task SendAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var sender = new FakeEmailSender();

        // Act & Assert
        await sender.Invoking(s => s.SendAsync(null!, "to@test.com", "Subject", "Body"))
            .Should().ThrowExactlyAsync<ArgumentNullException>();

        await sender.Invoking(s => s.SendAsync("from@test.com", null!, "Subject", "Body"))
            .Should().ThrowExactlyAsync<ArgumentNullException>();

        await sender.Invoking(s => s.SendAsync("from@test.com", "to@test.com", null!, "Body"))
            .Should().ThrowExactlyAsync<ArgumentNullException>();

        await sender.Invoking(s => s.SendAsync("from@test.com", "to@test.com", "Subject", null!))
            .Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_MultipleTimes_StoresAllEmails()
    {
        // Arrange
        var sender = new FakeEmailSender();

        // Act
        await sender.SendAsync("from1@test.com", "to1@test.com", "Subject 1", "Body 1");
        await sender.SendAsync("from2@test.com", "to2@test.com", "Subject 2", "Body 2");

        // Assert
        sender.Outbox.Should().HaveCount(2);
        sender.Outbox[0].Subject.Should().Be("Subject 1");
        sender.Outbox[1].Subject.Should().Be("Subject 2");
    }

    [Fact]
    public void FindBySubject_WithMatchingEmails_ReturnsMatchingEmails()
    {
        // Arrange
        var sender = new FakeEmailSender();
        sender.SendAsync("from@test.com", "to1@test.com", "Test Subject", "Body 1").Wait();
        sender.SendAsync("from@test.com", "to2@test.com", "Another Subject", "Body 2").Wait();
        sender.SendAsync("from@test.com", "to3@test.com", "Test Subject", "Body 3").Wait();

        // Act
        var results = sender.FindBySubject("Test Subject");

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.Subject == "Test Subject").Should().BeTrue();
    }

    [Fact]
    public void FindBySubject_CaseInsensitive_ReturnsMatchingEmails()
    {
        // Arrange
        var sender = new FakeEmailSender();
        sender.SendAsync("from@test.com", "to@test.com", "Test Subject", "Body").Wait();

        // Act
        var results = sender.FindBySubject("test subject");

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public void FindByRecipient_WithMatchingEmails_ReturnsMatchingEmails()
    {
        // Arrange
        var sender = new FakeEmailSender();
        sender.SendAsync("from@test.com", "to@test.com", "Subject 1", "Body 1").Wait();
        sender.SendAsync("from@test.com", "other@test.com", "Subject 2", "Body 2").Wait();
        sender.SendAsync("from@test.com", "to@test.com", "Subject 3", "Body 3").Wait();

        // Act
        var results = sender.FindByRecipient("to@test.com");

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.To == "to@test.com").Should().BeTrue();
    }

    [Fact]
    public void FindBySender_WithMatchingEmails_ReturnsMatchingEmails()
    {
        // Arrange
        var sender = new FakeEmailSender();
        sender.SendAsync("from1@test.com", "to@test.com", "Subject 1", "Body 1").Wait();
        sender.SendAsync("from2@test.com", "to@test.com", "Subject 2", "Body 2").Wait();
        sender.SendAsync("from1@test.com", "to@test.com", "Subject 3", "Body 3").Wait();

        // Act
        var results = sender.FindBySender("from1@test.com");

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.From == "from1@test.com").Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllEmails()
    {
        // Arrange
        var sender = new FakeEmailSender();
        sender.SendAsync("from@test.com", "to@test.com", "Subject", "Body").Wait();

        // Act
        sender.Clear();

        // Assert
        sender.Outbox.Should().BeEmpty();
        sender.Count.Should().Be(0);
        sender.HasEmails.Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfEmails()
    {
        // Arrange
        var sender = new FakeEmailSender();

        // Act & Assert
        sender.Count.Should().Be(0);

        sender.SendAsync("from@test.com", "to@test.com", "Subject", "Body").Wait();
        sender.Count.Should().Be(1);

        sender.SendAsync("from@test.com", "to@test.com", "Subject", "Body").Wait();
        sender.Count.Should().Be(2);
    }

    [Fact]
    public void HasEmails_ReturnsCorrectValue()
    {
        // Arrange
        var sender = new FakeEmailSender();

        // Act & Assert
        sender.HasEmails.Should().BeFalse();

        sender.SendAsync("from@test.com", "to@test.com", "Subject", "Body").Wait();
        sender.HasEmails.Should().BeTrue();

        sender.Clear();
        sender.HasEmails.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithFaultPolicy_AppliesFaultPolicy()
    {
        // Arrange
        var faultPolicy = FaultPolicy.AlwaysFail(() => new InvalidOperationException("Test fault"));
        var sender = new FakeEmailSender(faultPolicy: faultPolicy);

        // Act & Assert
        await sender.Invoking(s => s.SendAsync("from@test.com", "to@test.com", "Subject", "Body"))
            .Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("Test fault");

        sender.Outbox.Should().BeEmpty(); // Email should not be stored if fault occurs
    }

    [Fact]
    public async Task SendAsync_WithLatencyFaultPolicy_IntroducesDelay()
    {
        // Arrange
        var faultPolicy = FaultPolicy.WithLatency(TimeSpan.FromMilliseconds(50));
        var sender = new FakeEmailSender(faultPolicy: faultPolicy);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await sender.SendAsync("from@test.com", "to@test.com", "Subject", "Body");

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(40); // Allow for timing variance
        sender.Outbox.Should().HaveCount(1); // Email should still be stored
    }
}