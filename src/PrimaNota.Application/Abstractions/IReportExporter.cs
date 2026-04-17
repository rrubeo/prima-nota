namespace PrimaNota.Application.Abstractions;

/// <summary>Result of an export operation.</summary>
/// <param name="FileName">Suggested file name.</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="Content">File bytes.</param>
public sealed record ExportResult(string FileName, string ContentType, byte[] Content);
