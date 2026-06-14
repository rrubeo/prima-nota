using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.Integrazioni;

/// <summary>
/// Singleton aggregate holding the credentials and settings for the Aruba "Fatturazione
/// Elettronica" integration. The password is stored already encrypted (the application
/// layer protects it before persisting). Persisted with a fixed primary key so there is
/// always exactly one configuration row.
/// </summary>
public sealed class IntegrazioneAruba : AuditableEntity<int>
{
    /// <summary>Fixed primary-key value for the single row.</summary>
    public const int SingletonId = 1;

    /// <summary>Initializes a new instance of the <see cref="IntegrazioneAruba"/> class.</summary>
    public IntegrazioneAruba() => Id = SingletonId;

    /// <summary>Gets a value indicating whether the integration is enabled.</summary>
    public bool Abilitata { get; private set; }

    /// <summary>Gets the Aruba API username.</summary>
    public string? Username { get; private set; }

    /// <summary>Gets the encrypted Aruba API password (ciphertext, never the plaintext).</summary>
    public string? PasswordProtetta { get; private set; }

    /// <summary>Gets a value indicating whether to target the demo environment instead of production.</summary>
    public bool UsaDemo { get; private set; }

    /// <summary>Updates the configuration.</summary>
    /// <param name="abilitata">Whether the integration is enabled.</param>
    /// <param name="username">Aruba API username.</param>
    /// <param name="passwordProtetta">
    /// Encrypted password to store; pass <see langword="null"/> to keep the current one unchanged.
    /// </param>
    /// <param name="usaDemo">Whether to use the demo environment.</param>
    public void Configura(bool abilitata, string? username, string? passwordProtetta, bool usaDemo)
    {
        Abilitata = abilitata;
        Username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
        UsaDemo = usaDemo;

        if (passwordProtetta is not null)
        {
            PasswordProtetta = string.IsNullOrWhiteSpace(passwordProtetta) ? null : passwordProtetta;
        }
    }
}
