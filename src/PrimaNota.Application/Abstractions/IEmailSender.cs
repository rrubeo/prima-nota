namespace PrimaNota.Application.Abstractions;

/// <summary>Sends transactional emails (e.g. the two-factor login code).</summary>
public interface IEmailSender
{
    /// <summary>Gets a value indicating whether an email transport is configured and enabled.</summary>
    bool IsConfigured { get; }

    /// <summary>Sends an HTML email to a single recipient.</summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="subject">Subject line.</param>
    /// <param name="htmlBody">HTML body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
