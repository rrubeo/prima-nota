using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PrimaNota.Domain.Anagrafiche;
using PrimaNota.Domain.ContiFinanziari;
using PrimaNota.Domain.Esercizi;
using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;
using Movimento = PrimaNota.Domain.PrimaNota.MovimentoPrimaNota;

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

    /// <summary>Gets the <see cref="Anagrafica"/> set.</summary>
    DbSet<Anagrafica> Anagrafiche { get; }

    /// <summary>Gets the <see cref="Categoria"/> set.</summary>
    DbSet<Categoria> Categorie { get; }

    /// <summary>Gets the <see cref="Causale"/> set.</summary>
    DbSet<Causale> Causali { get; }

    /// <summary>Gets the <see cref="AliquotaIva"/> set.</summary>
    DbSet<AliquotaIva> AliquoteIva { get; }

    /// <summary>Gets the <see cref="ContoFinanziario"/> set.</summary>
    DbSet<ContoFinanziario> ContiFinanziari { get; }

    /// <summary>Gets the <see cref="Movimento"/> set.</summary>
    DbSet<Movimento> Movimenti { get; }

    /// <summary>Gets the <see cref="EsercizioContabile"/> set.</summary>
    DbSet<EsercizioContabile> Esercizi { get; }

    /// <summary>Persists pending changes to the underlying store.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
