using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Anagrafiche;
using PrimaNota.Domain.ContiFinanziari;
using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;
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

    /// <summary>Gets the <see cref="Categoria"/> set.</summary>
    public DbSet<Categoria> Categorie => Set<Categoria>();

    /// <summary>Gets the <see cref="Causale"/> set.</summary>
    public DbSet<Causale> Causali => Set<Causale>();

    /// <summary>Gets the <see cref="AliquotaIva"/> set.</summary>
    public DbSet<AliquotaIva> AliquoteIva => Set<AliquotaIva>();

    /// <summary>Gets the <see cref="ContoFinanziario"/> set.</summary>
    public DbSet<ContoFinanziario> ContiFinanziari => Set<ContoFinanziario>();

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
