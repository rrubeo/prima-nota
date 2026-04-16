using System.ComponentModel.DataAnnotations;

namespace PrimaNota.Infrastructure.Storage;

/// <summary>Configuration for the filesystem-backed attachment storage.</summary>
public sealed class AttachmentStorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Attachments";

    /// <summary>Gets or sets the absolute root directory where attachments are stored.</summary>
    [Required]
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum upload size in bytes (default 20 MB).</summary>
    [Range(1024, 524288000)]
    public long MaxSizeBytes { get; set; } = 20L * 1024 * 1024;

    /// <summary>Gets or sets the list of allowed MIME types (empty = allow all).</summary>
    public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();
}
