namespace PrimaNota.Domain.PrimaNota;

/// <summary>
/// File attached to a <see cref="MovimentoPrimaNota"/>. The physical bytes live on the
/// filesystem under the Attachments root; this entity only records the metadata and
/// the relative path, plus a SHA-256 hash for integrity verification.
/// </summary>
public sealed class Allegato
{
    /// <summary>Initializes a new instance of the <see cref="Allegato"/> class.</summary>
    /// <param name="nomeFile">Original file name (unsafe, sanitised at upload time).</param>
    /// <param name="mimeType">MIME type.</param>
    /// <param name="size">Size in bytes.</param>
    /// <param name="hashSha256">Hex SHA-256 digest of the file contents (64 chars).</param>
    /// <param name="pathRelativo">Relative path under the Attachments root where the bytes are stored.</param>
    /// <param name="uploadedAt">UTC upload timestamp.</param>
    /// <param name="uploadedBy">Identity user id of the uploader.</param>
    public Allegato(
        string nomeFile,
        string mimeType,
        long size,
        string hashSha256,
        string pathRelativo,
        DateTimeOffset uploadedAt,
        string? uploadedBy)
    {
        if (string.IsNullOrWhiteSpace(nomeFile))
        {
            throw new ArgumentException("Nome file obbligatorio.", nameof(nomeFile));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(size);

        if (string.IsNullOrWhiteSpace(hashSha256) || hashSha256.Length != 64)
        {
            throw new ArgumentException("Hash SHA-256 non valido (64 caratteri esadecimali attesi).", nameof(hashSha256));
        }

        if (string.IsNullOrWhiteSpace(pathRelativo))
        {
            throw new ArgumentException("Path relativo obbligatorio.", nameof(pathRelativo));
        }

        Id = Guid.NewGuid();
        NomeFile = nomeFile.Trim();
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim();
        Size = size;
        HashSha256 = hashSha256.ToLowerInvariant();
        PathRelativo = pathRelativo.Replace('\\', '/');
        UploadedAt = uploadedAt;
        UploadedBy = uploadedBy;
    }

    /// <summary>Initializes a new instance of the <see cref="Allegato"/> class for EF Core.</summary>
    private Allegato()
    {
    }

    /// <summary>Gets the attachment identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the parent movement identifier.</summary>
    public Guid MovimentoId { get; internal set; }

    /// <summary>Gets the original user-visible file name.</summary>
    public string NomeFile { get; private set; } = string.Empty;

    /// <summary>Gets the MIME type.</summary>
    public string MimeType { get; private set; } = "application/octet-stream";

    /// <summary>Gets the size in bytes.</summary>
    public long Size { get; private set; }

    /// <summary>Gets the hex SHA-256 digest (64 lowercase hex chars).</summary>
    public string HashSha256 { get; private set; } = string.Empty;

    /// <summary>Gets the storage path relative to the Attachments root.</summary>
    public string PathRelativo { get; private set; } = string.Empty;

    /// <summary>Gets the UTC upload timestamp.</summary>
    public DateTimeOffset UploadedAt { get; private set; }

    /// <summary>Gets the Identity user id of the uploader (if any).</summary>
    public string? UploadedBy { get; private set; }
}
