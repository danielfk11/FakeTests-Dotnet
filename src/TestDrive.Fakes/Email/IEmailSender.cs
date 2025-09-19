namespace TestDrive.Fakes.Email;

/// <summary>
/// Provides a contract for sending email messages.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    /// <param name="from">The sender's email address.</param>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="body">The email body content.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task SendAsync(string from, string to, string subject, string body, CancellationToken cancellationToken = default);
}