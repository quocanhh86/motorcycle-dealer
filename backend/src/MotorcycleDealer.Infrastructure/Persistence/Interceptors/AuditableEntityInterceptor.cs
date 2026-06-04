using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditableEntityInterceptor(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampAuditTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAuditTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAuditTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = utcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).IsModified = false;
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }

        foreach (var softDeleteEntry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (softDeleteEntry.State == EntityState.Modified &&
                softDeleteEntry.Entity.IsDeleted &&
                softDeleteEntry.Entity.DeletedAtUtc is null)
            {
                softDeleteEntry.Entity.DeletedAtUtc = utcNow;
            }
        }
    }
}
