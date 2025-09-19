using System.Collections.Concurrent;
using TestDrive.Fakes.Core;

namespace TestDrive.Fakes.Email;

/// <summary>
/// A fake implementation of <see cref="IEmailSender"/> that stores sent emails in memory.
/// Useful for testing email functionality without actually sending emails.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _outbox = new();
    private readonly IClock _clock;
    private readonly FaultPolicy _faultPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeEmailSender"/> class.
    /// </summary>
    /// <param name="clock">The clock to use for timestamping sent emails. If null, a default FixedClock will be used.</param>
    /// <param name="faultPolicy">The fault policy to apply when sending emails. If null, no faults will be introduced.</param>
    public FakeEmailSender(IClock? clock = null, FaultPolicy? faultPolicy = null)
    {
        _clock = clock ?? new FixedClock();
        _faultPolicy = faultPolicy ?? new FaultPolicy();
    }

    /// <summary>
    /// Gets a read-only list of all emails that have been sent through this sender.
    /// </summary>
    public IReadOnlyList<EmailMessage> Outbox => _outbox.ToArray();

    /// <summary>
    /// Sends an email message asynchronously by storing it in the outbox.
    /// </summary>
    /// <param name="from">The sender's email address.</param>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body content.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public async Task SendAsync(string from, string to, string subject, string body, CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_1
        ArgumentHelper.ThrowIfNull(from);
        ArgumentHelper.ThrowIfNull(to);
        ArgumentHelper.ThrowIfNull(subject);
        ArgumentHelper.ThrowIfNull(body);
#else
        ArgumentHelper.ThrowIfNull(from, nameof(from));
        ArgumentHelper.ThrowIfNull(to, nameof(to));
        ArgumentHelper.ThrowIfNull(subject, nameof(subject));
        ArgumentHelper.ThrowIfNull(body, nameof(body));
#endif

        // Apply fault policy (may introduce latency or throw exceptions)
        await _faultPolicy.ApplyAsync(cancellationToken).ConfigureAwait(false);

        // Create and store the email message
        var emailMessage = new EmailMessage(from, to, subject, body, _clock.UtcNow);
        _outbox.Enqueue(emailMessage);
    }

    /// <summary>
    /// Finds emails by subject using case-insensitive comparison.
    /// </summary>
    /// <param name="subject">The subject to search for.</param>
    /// <returns>A list of emails with matching subjects.</returns>
    public IReadOnlyList<EmailMessage> FindBySubject(string subject)
    {
        ArgumentHelper.ThrowIfNull(subject, nameof(subject));

        return _outbox
            .Where(email => string.Equals(email.Subject, subject, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Finds emails by recipient email address using case-insensitive comparison.
    /// </summary>
    /// <param name="to">The recipient email address to search for.</param>
    /// <returns>A list of emails sent to the specified recipient.</returns>
    public IReadOnlyList<EmailMessage> FindByRecipient(string to)
    {
        ArgumentHelper.ThrowIfNull(to, nameof(to));

        return _outbox
            .Where(email => string.Equals(email.To, to, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Finds emails by sender email address using case-insensitive comparison.
    /// </summary>
    /// <param name="from">The sender email address to search for.</param>
    /// <returns>A list of emails sent from the specified sender.</returns>
    public IReadOnlyList<EmailMessage> FindBySender(string from)
    {
        ArgumentHelper.ThrowIfNull(from, nameof(from));

        return _outbox
            .Where(email => string.Equals(email.From, from, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Clears all emails from the outbox.
    /// </summary>
    public void Clear()
    {
        while (_outbox.TryDequeue(out _))
        {
            // Keep dequeuing until the queue is empty
        }
    }

    /// <summary>
    /// Gets the number of emails in the outbox.
    /// </summary>
    public int Count => _outbox.Count;

    /// <summary>
    /// Determines whether the outbox contains any emails.
    /// </summary>
    /// <returns>true if the outbox contains emails; otherwise, false.</returns>
    public bool HasEmails => !_outbox.IsEmpty;
}