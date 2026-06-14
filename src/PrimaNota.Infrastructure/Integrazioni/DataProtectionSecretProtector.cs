using Microsoft.AspNetCore.DataProtection;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.Integrazioni;

/// <summary>
/// <see cref="ISecretProtector"/> backed by ASP.NET Core Data Protection. On IIS the keyring is
/// persisted outside the deployed folder, so protected secrets survive deploys and restarts.
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private readonly IDataProtector protector;

    /// <summary>Initializes a new instance of the <see cref="DataProtectionSecretProtector"/> class.</summary>
    /// <param name="provider">Data protection provider.</param>
    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        protector = provider.CreateProtector("PrimaNota.Integrazioni.Secrets.v1");
    }

    /// <inheritdoc />
    public string Protect(string plaintext) => protector.Protect(plaintext);

    /// <inheritdoc />
    public string Unprotect(string ciphertext) => protector.Unprotect(ciphertext);
}
