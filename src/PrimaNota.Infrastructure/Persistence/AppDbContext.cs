using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;

namespace PrimaNota.Infrastructure.Persistence;

/// <summary>
/// Main application <see cref="DbContext"/>. Aggregates for each feature module
/// are registered via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly, System.Func{System.Type, bool})"/>
/// as modules are implemented.
/// </summary>
public sealed class AppDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Initializes a new instance of the <see cref="AppDbContext"/> class.</summary>
    /// <param name="options">EF Core options supplied by the DI container.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
