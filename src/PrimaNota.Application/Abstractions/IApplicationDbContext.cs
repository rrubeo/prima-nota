using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PrimaNota.Application.Abstractions;

/// <summary>
/// Exposes the <see cref="DbContext"/> surface that the Application layer is allowed to touch.
/// Entity-specific <see cref="DbSet{TEntity}"/> properties are added here as modules are
/// implemented, preserving the dependency inversion between Application and Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Gets the EF Core database facade for transactions and raw SQL.</summary>
    DatabaseFacade Database { get; }

    /// <summary>Persists pending changes to the underlying store.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
