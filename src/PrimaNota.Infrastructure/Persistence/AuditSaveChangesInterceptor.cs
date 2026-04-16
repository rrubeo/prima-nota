using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Abstractions;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Infrastructure.Persistence;

/// <summary>
/// Populates <see cref="IAuditable"/> metadata (created/updated timestamps and user)
/// on every <c>SaveChanges</c> call.
/// </summary>
internal sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService currentUser;
    private readonly IDateTimeProvider clock;

    public AuditSaveChangesInterceptor(ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        this.currentUser = currentUser;
        this.clock = clock;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        var user = currentUser.UserId ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.CurrentValues[nameof(IAuditable.CreatedAt)] = now;
                    entry.CurrentValues[nameof(IAuditable.CreatedBy)] = user;
                    break;

                case EntityState.Modified:
                    entry.CurrentValues[nameof(IAuditable.UpdatedAt)] = now;
                    entry.CurrentValues[nameof(IAuditable.UpdatedBy)] = user;
                    break;
            }
        }
    }
}
