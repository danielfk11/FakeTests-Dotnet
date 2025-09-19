using TestDrive.Fakes.Email;

namespace TestDrive.Fakes.TestKit;

/// <summary>
/// Provides extension methods for making assertions about fake email senders in tests.
/// </summary>
public static class EmailAssertions
{
    /// <summary>
    /// Asserts that the email sender has sent the specified number of emails.
    /// </summary>
    /// <param name="emailSender">The fake email sender to check.</param>
    /// <param name="expectedCount">The expected number of sent emails.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailSender is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSent(this FakeEmailSender emailSender, int expectedCount)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));

        var actualCount = emailSender.Count;
        if (actualCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCount} emails to be sent, but {actualCount} were sent.");
        }
    }

    /// <summary>
    /// Asserts that the email sender has sent at least one email with the specified subject.
    /// </summary>
    /// <param name="emailSender">The fake email sender to check.</param>
    /// <param name="subject">The expected email subject.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailSender or subject is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSentEmailWithSubject(this FakeEmailSender emailSender, string subject)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));
        ArgumentHelper.ThrowIfNull(subject, nameof(subject));

        var emails = emailSender.FindBySubject(subject);
        if (emails.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected at least one email with subject '{subject}', but none were found.");
        }
    }

    /// <summary>
    /// Asserts that the email sender has sent at least one email to the specified recipient.
    /// </summary>
    /// <param name="emailSender">The fake email sender to check.</param>
    /// <param name="recipient">The expected recipient email address.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailSender or recipient is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSentEmailTo(this FakeEmailSender emailSender, string recipient)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));
        ArgumentHelper.ThrowIfNull(recipient, nameof(recipient));

        var emails = emailSender.FindByRecipient(recipient);
        if (emails.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected at least one email to '{recipient}', but none were found.");
        }
    }

    /// <summary>
    /// Asserts that the email sender has sent at least one email from the specified sender.
    /// </summary>
    /// <param name="emailSender">The fake email sender to check.</param>
    /// <param name="sender">The expected sender email address.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailSender or sender is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSentEmailFrom(this FakeEmailSender emailSender, string sender)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));
        ArgumentHelper.ThrowIfNull(sender, nameof(sender));

        var emails = emailSender.FindBySender(sender);
        if (emails.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected at least one email from '{sender}', but none were found.");
        }
    }

    /// <summary>
    /// Asserts that the email sender has sent an email with the specified subject to the specified recipient.
    /// </summary>
    /// <param name="emailSender">The fake email sender to check.</param>
    /// <param name="recipient">The expected recipient email address.</param>
    /// <param name="subject">The expected email subject.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSentEmailTo(this FakeEmailSender emailSender, string recipient, string subject)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));
        ArgumentHelper.ThrowIfNull(recipient, nameof(recipient));
        ArgumentHelper.ThrowIfNull(subject, nameof(subject));

        var matchingEmails = emailSender.Outbox
            .Where(email => string.Equals(email.To, recipient, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(email.Subject, subject, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingEmails.Count == 0)
        {
            throw new InvalidOperationException(
                $"Expected at least one email to '{recipient}' with subject '{subject}', but none were found.");
        }
    }

    /// <summary>
    /// Asserts that the email sender has not sent any emails.
    /// </summary>
    /// <param name="emailSender">The fake email sender to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when emailSender is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the assertion fails.</exception>
    public static void ShouldNotHaveSentAnyEmails(this FakeEmailSender emailSender)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));

        if (emailSender.HasEmails)
        {
            throw new InvalidOperationException(
                $"Expected no emails to be sent, but {emailSender.Count} were sent.");
        }
    }

    /// <summary>
    /// Gets the first email with the specified subject, or throws an exception if not found.
    /// </summary>
    /// <param name="emailSender">The fake email sender to search.</param>
    /// <param name="subject">The subject to search for.</param>
    /// <returns>The first email with the specified subject.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no email with the subject is found.</exception>
    public static EmailMessage GetEmailWithSubject(this FakeEmailSender emailSender, string subject)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));
        ArgumentHelper.ThrowIfNull(subject, nameof(subject));

        var emails = emailSender.FindBySubject(subject);
        if (emails.Count == 0)
        {
            throw new InvalidOperationException($"No email found with subject '{subject}'.");
        }

        return emails[0];
    }

    /// <summary>
    /// Gets the first email sent to the specified recipient, or throws an exception if not found.
    /// </summary>
    /// <param name="emailSender">The fake email sender to search.</param>
    /// <param name="recipient">The recipient to search for.</param>
    /// <returns>The first email sent to the specified recipient.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no email to the recipient is found.</exception>
    public static EmailMessage GetEmailTo(this FakeEmailSender emailSender, string recipient)
    {
        ArgumentHelper.ThrowIfNull(emailSender, nameof(emailSender));
        ArgumentHelper.ThrowIfNull(recipient, nameof(recipient));

        var emails = emailSender.FindByRecipient(recipient);
        if (emails.Count == 0)
        {
            throw new InvalidOperationException($"No email found for recipient '{recipient}'.");
        }

        return emails[0];
    }
}