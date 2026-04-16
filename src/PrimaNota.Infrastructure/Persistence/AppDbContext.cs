using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Anagrafiche;
using PrimaNota.Infrastructure.Identity;

namespace PrimaNota.Infrastructure.Persistence;

/// <summary>
/// Main application <see cref="DbContext"/>. Combines ASP.NET Core Identity tables
/// (under schema <c>identity</c>) with domain aggregates (under schema <c>app</c>).
/// </summary>
public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>,
      IApplicationDbContext
{
    /// <summary>Initializes a new instance of the <see cref="AppDbContext"/> class.</summary>
    /// <param name="options">EF Core options supplied by the DI container.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the <see cref="Anagrafica"/> set.</summary>
    public DbSet<Anagrafica> Anagrafiche => Set<Anagrafica>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasDefaultSchema("app");

        base.OnModelCreating(builder);

        // Relocate Identity tables into their own schema to keep the app schema domain-pure.
        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => e.ClrType.Namespace == typeof(ApplicationUser).Namespace
                        || e.ClrType.Namespace?.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal) == true))
        {
            entity.SetSchema("identity");
        }

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
