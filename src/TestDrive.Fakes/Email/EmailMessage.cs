using TestDrive.Fakes.Core;

namespace TestDrive.Fakes.Email;

/// <summary>
/// Represents an email message that was sent through the fake email sender.
/// </summary>
public sealed class EmailMessage
{
    /// <summary>
    /// Gets the sender's email address.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Gets the recipient's email address.
    /// </summary>
    public string To { get; }

    /// <summary>
    /// Gets the email subject.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Gets the email body content.
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Gets the UTC timestamp when the email was sent.
    /// </summary>
    public DateTime UtcSentAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailMessage"/> class.
    /// </summary>
    /// <param name="from">The sender's email address.</param>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body content.</param>
    /// <param name="utcSentAt">The UTC timestamp when the email was sent.</param>
    public EmailMessage(string from, string to, string subject, string body, DateTime utcSentAt)
    {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        UtcSentAt = utcSentAt;
    }

    /// <summary>
    /// Returns a string representation of the email message.
    /// </summary>
    /// <returns>A string containing the email details.</returns>
    public override string ToString()
    {
        return $"From: {From}, To: {To}, Subject: {Subject}, Sent: {UtcSentAt:yyyy-MM-dd HH:mm:ss} UTC";
    }
}