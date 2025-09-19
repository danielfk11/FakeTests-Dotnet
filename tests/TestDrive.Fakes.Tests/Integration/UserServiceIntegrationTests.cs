namespace TestDrive.Fakes.Tests.Integration;

/// <summary>
/// Integration test that simulates a user service using multiple fake components.
/// </summary>
public sealed class UserServiceIntegrationTests
{
    [Fact]
    public async Task CreateUser_IntegrationTest_UsesAllFakeComponents()
    {
        // Arrange
        var clock = new FixedClock(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        var idGenerator = new DeterministicIdGenerator();
        var emailSender = new FakeEmailSender(clock);
        var storage = new InMemoryBlobStorage(clock);

        var userService = new UserService(clock, idGenerator, emailSender, storage);

        // Act
        var userId = await userService.CreateUserAsync("john.doe@example.com", "John Doe", "avatar-data"u8.ToArray());

        // Assert
        // Verify ID generation
        userId.Should().Be("000001");
        idGenerator.CurrentValue.Should().Be(1);

        // Verify email was sent
        emailSender.ShouldHaveSent(1);
        emailSender.ShouldHaveSentEmailTo("john.doe@example.com", "Welcome!");
        var welcomeEmail = emailSender.GetEmailTo("john.doe@example.com");
        welcomeEmail.Body.Should().Contain("Welcome to our service, John Doe!");
        welcomeEmail.UtcSentAt.Should().Be(clock.UtcNow);

        // Verify avatar was stored
        storage.ShouldContainBlob("avatars", "000001.jpg");
        storage.ShouldHaveContentType("avatars", "000001.jpg", "image/jpeg");
        storage.ShouldHaveContent("avatars", "000001.jpg", "avatar-data"u8.ToArray());

        var avatar = storage.GetBlob("avatars", "000001.jpg");
        avatar.UploadedAt.Should().Be(clock.UtcNow);
    }

    [Fact]
    public async Task CreateMultipleUsers_GeneratesSequentialIdsAndEmails()
    {
        // Arrange
        var clock = new FixedClock(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        var idGenerator = new DeterministicIdGenerator();
        var emailSender = new FakeEmailSender(clock);
        var storage = new InMemoryBlobStorage(clock);

        var userService = new UserService(clock, idGenerator, emailSender, storage);

        // Act
        var user1Id = await userService.CreateUserAsync("user1@example.com", "User One", "avatar1"u8.ToArray());
        
        clock.Advance(TimeSpan.FromMinutes(5));
        var user2Id = await userService.CreateUserAsync("user2@example.com", "User Two", "avatar2"u8.ToArray());

        // Assert
        user1Id.Should().Be("000001");
        user2Id.Should().Be("000002");

        emailSender.ShouldHaveSent(2);
        emailSender.ShouldHaveSentEmailTo("user1@example.com");
        emailSender.ShouldHaveSentEmailTo("user2@example.com");

        storage.ShouldHaveObjectCount("avatars", 2);
        storage.ShouldContainBlob("avatars", "000001.jpg");
        storage.ShouldContainBlob("avatars", "000002.jpg");

        // Verify timestamps are different
        var avatar1 = storage.GetBlob("avatars", "000001.jpg");
        var avatar2 = storage.GetBlob("avatars", "000002.jpg");
        avatar2.UploadedAt.Should().BeAfter(avatar1.UploadedAt);
    }
}

/// <summary>
/// Simulated user service for integration testing.
/// This represents a typical service that would use multiple dependencies.
/// </summary>
public sealed class UserService
{
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IBlobStorage _storage;

    public UserService(IClock clock, IIdGenerator idGenerator, IEmailSender emailSender, IBlobStorage storage)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<string> CreateUserAsync(string email, string displayName, byte[] avatarData)
    {
        // Generate unique user ID
        var userId = _idGenerator.GenerateId();

        // Store avatar image
        var avatarKey = $"{userId}.jpg";
        using var avatarStream = new MemoryStream(avatarData);
        await _storage.UploadAsync("avatars", avatarKey, avatarStream, "image/jpeg");

        // Send welcome email
        var emailSubject = "Welcome!";
        var emailBody = $"Welcome to our service, {displayName}! Your user ID is {userId}.";
        await _emailSender.SendAsync("noreply@example.com", email, emailSubject, emailBody);

        return userId;
    }
}