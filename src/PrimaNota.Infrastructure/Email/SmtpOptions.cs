using System.ComponentModel.DataAnnotations;

namespace PrimaNota.Infrastructure.Email;

/// <summary>Configuration for the SMTP email transport (bound from the <c>Smtp</c> section).</summary>
public sealed class SmtpOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Smtp";

    /// <summary>Gets or sets a value indicating whether email sending is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the SMTP server host name.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the SMTP server port (default 587 for STARTTLS).</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>Gets or sets the SMTP username (optional for anonymous relays).</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the SMTP password (optional for anonymous relays).</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the sender email address (envelope from).</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional sender display name.</summary>
    public string? FromName { get; set; }

    /// <summary>Gets or sets a value indicating whether to use STARTTLS (true) or implicit SSL (false).</summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>Gets a value indicating whether the transport is usable (enabled + host + sender present).</summary>
    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
