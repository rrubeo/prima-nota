namespace PrimaNota.Application.Abstractions;

/// <summary>
/// Persists and retrieves attachment bytes. Implementations typically use the server
/// filesystem (encrypted via EFS/BitLocker in production) under a configurable root.
/// </summary>
public interface IAttachmentStorage
{
    /// <summary>Persists a stream of bytes under a namespaced folder.</summary>
    /// <param name="subfolder">Logical folder (e.g. "movimenti/2026" or "note-spese").</param>
    /// <param name="originalFileName">Client-supplied file name (sanitised by the implementation).</param>
    /// <param name="content">Readable content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The write result with relative path, hash and size.</returns>
    Task<AttachmentWriteResult> SaveAsync(
        string subfolder,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a read stream for an existing attachment.</summary>
    /// <param name="relativePath">Path as returned by <see cref="SaveAsync"/>.</param>
    /// <returns>An open read stream; caller disposes.</returns>
    Stream OpenRead(string relativePath);

    /// <summary>Removes an attachment from storage. Missing files are silently ignored.</summary>
    /// <param name="relativePath">Path as returned by <see cref="SaveAsync"/>.</param>
    void Delete(string relativePath);
}
