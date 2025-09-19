using System.Text;
using TestDrive.Fakes.Core;
using TestDrive.Fakes.Email;
using TestDrive.Fakes.Storage;
using TestDrive.Fakes.Http;

namespace Sample.Console;

class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== TestDrive.Fakes Sample Application ===");
        System.Console.WriteLine();

        await DemonstrateEmailSender();
        System.Console.WriteLine();

        await DemonstrateBlobStorage();
        System.Console.WriteLine();

        await DemonstrateHttpHandler();
        System.Console.WriteLine();

        DemonstrateClockAndIdGenerator();
        System.Console.WriteLine();

        System.Console.WriteLine("=== Sample completed successfully! ===");
    }

    static async Task DemonstrateEmailSender()
    {
        System.Console.WriteLine("📧 Email Sender Demo:");
        System.Console.WriteLine("---");

        // Create fake email sender with fixed clock for predictable timestamps
        var clock = new FixedClock(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        var emailSender = new FakeEmailSender(clock);

        // Send some emails
        await emailSender.SendAsync("noreply@example.com", "user1@test.com", "Welcome!", "Welcome to our service!");
        await emailSender.SendAsync("support@example.com", "user2@test.com", "Password Reset", "Click here to reset your password.");
        
        // Advance time and send another email
        clock.Advance(TimeSpan.FromMinutes(30));
        await emailSender.SendAsync("newsletter@example.com", "user1@test.com", "Monthly Newsletter", "Here's what happened this month...");

        // Display results
        System.Console.WriteLine($"📬 Total emails sent: {emailSender.Count}");
        System.Console.WriteLine();

        foreach (var email in emailSender.Outbox)
        {
            System.Console.WriteLine($"  To: {email.To}");
            System.Console.WriteLine($"  Subject: {email.Subject}");
            System.Console.WriteLine($"  Sent at: {email.UtcSentAt:yyyy-MM-dd HH:mm:ss} UTC");
            System.Console.WriteLine();
        }

        // Demonstrate search functionality
        var welcomeEmails = emailSender.FindBySubject("Welcome!");
        System.Console.WriteLine($"📬 Found {welcomeEmails.Count} welcome email(s)");

        var user1Emails = emailSender.FindByRecipient("user1@test.com");
        System.Console.WriteLine($"📬 Found {user1Emails.Count} email(s) for user1@test.com");
    }

    static async Task DemonstrateBlobStorage()
    {
        System.Console.WriteLine("💾 Blob Storage Demo:");
        System.Console.WriteLine("---");

        var storage = new InMemoryBlobStorage();

        // Upload some files
        await UploadTextFile(storage, "documents", "readme.txt", "This is a sample README file.");
        await UploadTextFile(storage, "documents", "config.json", """{"setting1": "value1", "setting2": "value2"}""");
        await UploadTextFile(storage, "images", "avatar.jpg", "fake-image-data");
        await UploadTextFile(storage, "logs", "app.log", "2023-01-15 10:30:00 - Application started");

        // List all buckets
        var buckets = storage.GetBucketNames();
        System.Console.WriteLine($"📁 Found {buckets.Count} bucket(s): {string.Join(", ", buckets)}");
        System.Console.WriteLine();

        // List files in each bucket
        foreach (var bucket in buckets)
        {
            var keys = await storage.ListKeysAsync(bucket);
            System.Console.WriteLine($"📁 Bucket '{bucket}' contains {keys.Count} file(s):");
            
            foreach (var key in keys)
            {
                var blob = storage.GetAllBlobs(bucket)[key];
                System.Console.WriteLine($"  📄 {key} ({blob.Size} bytes, {blob.ContentType})");
            }
            System.Console.WriteLine();
        }

        // Download and display a file
        using var downloadStream = await storage.DownloadAsync("documents", "readme.txt");
        if (downloadStream != null)
        {
            using var reader = new StreamReader(downloadStream);
            var content = await reader.ReadToEndAsync();
            System.Console.WriteLine($"📄 Content of readme.txt: {content}");
        }

        System.Console.WriteLine($"💾 Total objects: {storage.TotalObjectCount}");
        System.Console.WriteLine($"💾 Total size: {storage.TotalSizeInBytes} bytes");
    }

    static async Task DemonstrateHttpHandler()
    {
        System.Console.WriteLine("🌐 HTTP Handler Demo:");
        System.Console.WriteLine("---");

        // Create fake HTTP handler with some rules
        var httpHandler = new FakeHttpHandler();
        
        // Configure different endpoints
        httpHandler
            .WhenGet("https://api.example.com/status", """{"status": "ok", "timestamp": "2023-01-15T10:30:00Z"}""")
            .WhenGet("https://api.example.com/users/123", """{"id": 123, "name": "John Doe", "email": "john@example.com"}""")
            .WhenPost("https://api.example.com/users", """{"id": 456, "message": "User created successfully"}""")
            .When(req => req.RequestUri!.ToString().Contains("search"), 
                 req => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                 {
                     Content = new StringContent("""{"results": ["item1", "item2", "item3"]}""", Encoding.UTF8, "application/json")
                 });

        // Create HTTP client with fake handler
        using var httpClient = new HttpClient(httpHandler);

        // Make various HTTP requests
        var statusResponse = await httpClient.GetAsync("https://api.example.com/status");
        var statusContent = await statusResponse.Content.ReadAsStringAsync();
        System.Console.WriteLine($"📡 GET /status: {statusResponse.StatusCode}");
        System.Console.WriteLine($"📄 Response: {statusContent}");
        System.Console.WriteLine();

        var userResponse = await httpClient.GetAsync("https://api.example.com/users/123");
        var userContent = await userResponse.Content.ReadAsStringAsync();
        System.Console.WriteLine($"📡 GET /users/123: {userResponse.StatusCode}");
        System.Console.WriteLine($"📄 Response: {userContent}");
        System.Console.WriteLine();

        var createResponse = await httpClient.PostAsync("https://api.example.com/users", new StringContent(""));
        var createContent = await createResponse.Content.ReadAsStringAsync();
        System.Console.WriteLine($"📡 POST /users: {createResponse.StatusCode}");
        System.Console.WriteLine($"📄 Response: {createContent}");
        System.Console.WriteLine();

        var searchResponse = await httpClient.GetAsync("https://api.example.com/search?q=test");
        var searchContent = await searchResponse.Content.ReadAsStringAsync();
        System.Console.WriteLine($"📡 GET /search: {searchResponse.StatusCode}");
        System.Console.WriteLine($"📄 Response: {searchContent}");
        System.Console.WriteLine();

        // Try an unmatched request
        var unmatchedResponse = await httpClient.GetAsync("https://api.example.com/unknown");
        System.Console.WriteLine($"📡 GET /unknown: {unmatchedResponse.StatusCode}");
        var unmatchedContent = await unmatchedResponse.Content.ReadAsStringAsync();
        System.Console.WriteLine($"📄 Response: {unmatchedContent}");
    }

    static void DemonstrateClockAndIdGenerator()
    {
        System.Console.WriteLine("⏰ Clock and ID Generator Demo:");
        System.Console.WriteLine("---");

        // Create fixed clock and demonstrate time manipulation
        var clock = new FixedClock(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        System.Console.WriteLine($"⏰ Initial time: {clock.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        clock.Advance(TimeSpan.FromHours(2));
        System.Console.WriteLine($"⏰ After advancing 2 hours: {clock.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        clock.Advance(TimeSpan.FromMinutes(30));
        System.Console.WriteLine($"⏰ After advancing 30 minutes: {clock.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        System.Console.WriteLine();

        // Create ID generator and demonstrate sequential IDs
        var idGenerator = new DeterministicIdGenerator();
        System.Console.WriteLine("🆔 Generated IDs:");
        
        for (int i = 0; i < 5; i++)
        {
            var id = idGenerator.GenerateId();
            System.Console.WriteLine($"  ID #{i + 1}: {id}");
        }

        System.Console.WriteLine($"🆔 Current counter value: {idGenerator.CurrentValue}");

        // Reset and generate more
        idGenerator.Reset();
        System.Console.WriteLine("🆔 After reset:");
        System.Console.WriteLine($"  Next ID: {idGenerator.GenerateId()}");
        System.Console.WriteLine($"  Next ID: {idGenerator.GenerateId()}");
    }

    static async Task UploadTextFile(InMemoryBlobStorage storage, string bucket, string key, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        await storage.UploadAsync(bucket, key, stream, "text/plain");
    }
}