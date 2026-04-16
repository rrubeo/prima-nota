using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.Storage;

/// <summary>
/// Filesystem-backed <see cref="IAttachmentStorage"/>. Files are stored under the
/// configured root as: <c>&lt;root&gt;/&lt;subfolder&gt;/&lt;guid&gt;.&lt;ext&gt;</c>.
/// Computes SHA-256 while streaming to avoid a double read.
/// </summary>
internal sealed class FileSystemAttachmentStorage : IAttachmentStorage
{
    private readonly AttachmentStorageOptions options;
    private readonly ILogger<FileSystemAttachmentStorage> logger;

    public FileSystemAttachmentStorage(
        IOptions<AttachmentStorageOptions> options,
        ILogger<FileSystemAttachmentStorage> logger)
    {
        this.options = options.Value;
        this.logger = logger;

        if (string.IsNullOrWhiteSpace(this.options.RootPath))
        {
            throw new InvalidOperationException("Attachments:RootPath is required.");
        }

        Directory.CreateDirectory(this.options.RootPath);
    }

    /// <inheritdoc />
    public async Task<AttachmentWriteResult> SaveAsync(
        string subfolder,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(subfolder))
        {
            throw new ArgumentException("Subfolder required.", nameof(subfolder));
        }

        var safeSubfolder = SanitizeSubfolder(subfolder);
        var absoluteFolder = Path.Combine(options.RootPath, safeSubfolder);
        Directory.CreateDirectory(absoluteFolder);

        var extension = SanitizeExtension(Path.GetExtension(originalFileName));
        var storedFileName = Guid.NewGuid().ToString("N") + extension;
        var absoluteFilePath = Path.Combine(absoluteFolder, storedFileName);
        var relativePath = Path.Combine(safeSubfolder, storedFileName).Replace('\\', '/');

        using var sha = SHA256.Create();
        await using var target = new FileStream(
            absoluteFilePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        long totalWritten = 0;
        var buffer = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (totalWritten + read > options.MaxSizeBytes)
            {
                target.Close();
                File.Delete(absoluteFilePath);
                throw new InvalidOperationException(
                    $"Allegato oltre il limite consentito ({options.MaxSizeBytes:N0} byte).");
            }

            sha.TransformBlock(buffer, 0, read, null, 0);
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalWritten += read;
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hex = Convert.ToHexString(sha.Hash!).ToLowerInvariant();

        logger.LogInformation(
            "Stored attachment {RelativePath} ({Size} bytes, sha256 {Sha})",
            relativePath,
            totalWritten,
            hex);

        return new AttachmentWriteResult(relativePath, hex, totalWritten);
    }

    /// <inheritdoc />
    public Stream OpenRead(string relativePath)
    {
        var absolute = ResolveAbsolutePath(relativePath);
        return new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <inheritdoc />
    public void Delete(string relativePath)
    {
        try
        {
            var absolute = ResolveAbsolutePath(relativePath);
            if (File.Exists(absolute))
            {
                File.Delete(absolute);
            }
        }
#pragma warning disable CA1031 // delete must never fault the caller — log and continue
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogWarning(ex, "Delete attachment failed for {Path}", relativePath);
        }
    }

    private string ResolveAbsolutePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path required.", nameof(relativePath));
        }

        var combined = Path.GetFullPath(Path.Combine(options.RootPath, relativePath));
        var root = Path.GetFullPath(options.RootPath);

        // Guard against directory traversal via crafted relative paths.
        if (!combined.StartsWith(root, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Path outside attachment root.");
        }

        return combined;
    }

    private static string SanitizeSubfolder(string subfolder)
    {
        var parts = subfolder.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine(parts.Select(SanitizeSegment).ToArray());
    }

    private static string SanitizeSegment(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(segment.Where(c => !invalid.Contains(c) && c != '.').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "_" : safe;
    }

    private static string SanitizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 10)
        {
            return string.Empty;
        }

        var invalid = Path.GetInvalidFileNameChars();
        return new string(extension.Where(c => !invalid.Contains(c)).ToArray()).ToLowerInvariant();
    }
}
