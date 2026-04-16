namespace PrimaNota.Infrastructure.Configuration;

/// <summary>Configuration for Google OAuth 2.0 sign-in.</summary>
public sealed class GoogleAuthenticationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Authentication:Google";

    /// <summary>Gets or sets the OAuth client id issued by Google.</summary>
    public string? ClientId { get; set; }

    /// <summary>Gets or sets the OAuth client secret issued by Google.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Gets a value indicating whether Google auth is configured.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
