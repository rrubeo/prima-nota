using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.Email;

/// <summary>MailKit-based <see cref="IEmailSender"/> that delivers mail through an SMTP server.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions options;
    private readonly ILogger<SmtpEmailSender> logger;

    /// <summary>Initializes a new instance of the <see cref="SmtpEmailSender"/> class.</summary>
    /// <param name="options">SMTP options.</param>
    /// <param name="logger">Logger.</param>
    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
        this.logger = logger;
    }

    /// <inheritdoc />
    public bool IsConfigured => options.IsConfigured;

    /// <inheritdoc />
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
        {
            throw new InvalidOperationException(
                "Invio email non configurato: imposta la sezione 'Smtp' (Enabled/Host/FromAddress) in appsettings.");
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Destinatario obbligatorio.", nameof(toEmail));
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName ?? options.FromAddress, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        var secureOption = options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host, options.Port, secureOption, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            await client.AuthenticateAsync(options.Username, options.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Email '{Subject}' inviata a {Recipient}.", subject, toEmail);
    }
}
