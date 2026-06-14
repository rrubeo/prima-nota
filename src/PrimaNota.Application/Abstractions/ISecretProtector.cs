namespace PrimaNota.Application.Abstractions;

/// <summary>Encrypts and decrypts small secrets (e.g. integration passwords) stored in the database.</summary>
public interface ISecretProtector
{
    /// <summary>Encrypts a plaintext secret into an opaque, storable ciphertext.</summary>
    /// <param name="plaintext">The secret in clear.</param>
    /// <returns>Protected ciphertext.</returns>
    string Protect(string plaintext);

    /// <summary>Decrypts a ciphertext produced by <see cref="Protect"/>.</summary>
    /// <param name="ciphertext">Protected ciphertext.</param>
    /// <returns>The original plaintext.</returns>
    string Unprotect(string ciphertext);
}
