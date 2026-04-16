namespace PrimaNota.Application.Abstractions;

/// <summary>Result of a successful attachment write.</summary>
/// <param name="RelativePath">Path relative to the storage root, to be persisted on the aggregate.</param>
/// <param name="HashSha256">Hex SHA-256 digest of the written bytes.</param>
/// <param name="Size">Size in bytes.</param>
public sealed record AttachmentWriteResult(string RelativePath, string HashSha256, long Size);
