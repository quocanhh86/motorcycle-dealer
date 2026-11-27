using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Enforces ADR-003 4-table immutability rules at the EF Core ChangeTracker boundary:
/// - Append-only ledger entities (IAppendOnlyEntity / PointLedgerEntries) must NEVER be Modified or Deleted.
/// - Immutable financial records (IImmutableFinancialEntity / SalesOrders, ServiceInvoices, ServiceInvoiceLineItems) must NEVER be Modified or Deleted.
/// </summary>
public sealed class AppendOnlyLedgerInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> AppendOnlyLedgerNames = new(StringComparer.Ordinal)
    {
        "PointLedgerEntry",
        "PointLedgerEntries"
    };

    private static readonly HashSet<string> FullyImmutableRecordNames = new(StringComparer.Ordinal)
    {
        "SalesOrder",
        "SalesOrders",
        "ServiceInvoice",
        "ServiceInvoices",
        "ServiceInvoiceLineItem",
        "ServiceInvoiceLineItems"
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        GuardAgainstIllegalMutations(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        GuardAgainstIllegalMutations(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void GuardAgainstIllegalMutations(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var entityTypeName = entry.Metadata.ClrType.Name;
            var tableName = entry.Metadata.GetTableName() ?? string.Empty;

            var isAppendOnlyLedger = entry.Entity is IAppendOnlyEntity ||
                                     AppendOnlyLedgerNames.Contains(entityTypeName) ||
                                     AppendOnlyLedgerNames.Contains(tableName);

            if (isAppendOnlyLedger)
            {
                throw new DomainException(
                    "IMMUTABLE_LEDGER_VIOLATION",
                    $"Append-only ledger entity '{entityTypeName}' cannot be {entry.State.ToString().ToLowerInvariant()}. Append a compensating ledger entry instead (ADR-003).");
            }

            var isFullyImmutableRecord = entry.Entity is IImmutableFinancialEntity ||
                                         FullyImmutableRecordNames.Contains(entityTypeName) ||
                                         FullyImmutableRecordNames.Contains(tableName);

            if (isFullyImmutableRecord)
            {
                throw new DomainException(
                    "IMMUTABLE_FINANCIAL_RECORD_VIOLATION",
                    $"Immutable financial record '{entityTypeName}' cannot be {entry.State.ToString().ToLowerInvariant()} (ADR-003).");
            }
        }
    }
}
