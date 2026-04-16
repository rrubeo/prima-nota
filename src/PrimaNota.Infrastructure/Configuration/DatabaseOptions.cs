using System.ComponentModel.DataAnnotations;

namespace PrimaNota.Infrastructure.Configuration;

/// <summary>Strongly-typed configuration binding for database-related settings.</summary>
public sealed class DatabaseOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Database";

    /// <summary>Gets or sets the SQL Server connection string for the application DB.</summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command timeout (in seconds) applied to every EF Core command.
    /// Defaults to 30 seconds.
    /// </summary>
    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
